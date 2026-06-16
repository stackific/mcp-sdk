using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Stackific.Mcp.Json;
using Stackific.Mcp.JsonRpc;
using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Transport;

/// <summary>
/// The result of an authorization gate (spec §23): either an authorized identity, or a challenge to
/// return to the client (an HTTP status and a <c>WWW-Authenticate</c> header).
/// </summary>
/// <param name="Authorized">Whether the request is authorized.</param>
/// <param name="Identity">The validated identity when <paramref name="Authorized"/> is true.</param>
/// <param name="ChallengeStatus">The HTTP status to return when not authorized (for example 401).</param>
/// <param name="WwwAuthenticate">The <c>WWW-Authenticate</c> header value to return when not authorized.</param>
public sealed record AuthGateResult(bool Authorized, AuthInfo? Identity, int ChallengeStatus = 401, string? WwwAuthenticate = null);

/// <summary>
/// A pluggable authorization gate the Streamable HTTP adapter consults before dispatching a request
/// (spec §23). An unauthenticated request is answered with a 401 challenge carrying
/// <c>WWW-Authenticate</c>; an authenticated request yields the validated <see cref="AuthInfo"/>.
/// </summary>
public interface IMcpAuthGate
{
  /// <summary>Authorizes an incoming HTTP request.</summary>
  /// <param name="context">The HTTP context.</param>
  /// <returns>The gate result (authorized identity or a challenge).</returns>
  Task<AuthGateResult> AuthorizeAsync(HttpContext context);
}

/// <summary>
/// The server-side Streamable HTTP adapter (spec §9): it parses an HTTP POST into a JSON-RPC message,
/// validates the required request and routing headers (§9.3/§9.4), runs an optional authorization gate
/// (§23), dispatches to an <see cref="IMcpRequestHandler"/>, and writes back either a single JSON
/// response (§9.6.1) or a server-sent event stream that carries request-scoped notifications before the
/// final response (§9.6.2). It is stateless and never mints a session id (§9.9).
/// </summary>
public static class StreamableHttpServer
{
  /// <summary>Maps the MCP endpoint at <paramref name="pattern"/> onto <paramref name="handler"/>.</summary>
  /// <param name="endpoints">The endpoint route builder.</param>
  /// <param name="pattern">The route pattern (for example <c>/mcp</c>).</param>
  /// <param name="handler">The request handler (an <c>McpServer</c>).</param>
  /// <param name="authGate">An optional authorization gate (§23).</param>
  /// <returns>The endpoint convention builder for further configuration.</returns>
  public static IEndpointConventionBuilder MapMcp(
    this IEndpointRouteBuilder endpoints,
    string pattern,
    IMcpRequestHandler handler,
    IMcpAuthGate? authGate = null)
  {
    // Accept every method so the adapter itself can answer GET/DELETE with 405 (§9.9).
    return endpoints.MapMethods(pattern, ["GET", "POST", "DELETE", "PUT", "PATCH"],
      (HttpContext context) => HandleAsync(context, handler, authGate));
  }

  /// <summary>Handles a single HTTP request against <paramref name="handler"/> (spec §9).</summary>
  /// <param name="context">The HTTP context.</param>
  /// <param name="handler">The request handler.</param>
  /// <param name="authGate">An optional authorization gate.</param>
  /// <returns>A task that completes when the response has been written.</returns>
  public static async Task HandleAsync(HttpContext context, IMcpRequestHandler handler, IMcpAuthGate? authGate = null)
  {
    var request = context.Request;
    if (!HttpMethods.IsPost(request.Method))
    {
      // §9.9: a Streamable-HTTP-only server answers GET/DELETE at the endpoint with 405.
      context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
      return;
    }

    string body;
    using (var reader = new StreamReader(request.Body, Encoding.UTF8))
    {
      body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
    }

    JsonRpcMessage message;
    try
    {
      message = JsonRpcMessageSerializer.Parse(body);
    }
    catch (McpError error)
    {
      await WriteSingleErrorAsync(context, null, error).ConfigureAwait(false);
      return;
    }

    switch (message)
    {
      case JsonRpcNotification notification:
        await handler.HandleNotificationAsync(notification, context.RequestAborted).ConfigureAwait(false);
        context.Response.StatusCode = StatusCodes.Status202Accepted;
        return;

      case JsonRpcRequest jsonRpcRequest:
        await HandleRequestAsync(context, handler, authGate, jsonRpcRequest).ConfigureAwait(false);
        return;

      default:
        await WriteSingleErrorAsync(context, null, McpError.InvalidRequest("A client MUST NOT send a JSON-RPC response to the server.")).ConfigureAwait(false);
        return;
    }
  }

  private static async Task HandleRequestAsync(HttpContext context, IMcpRequestHandler handler, IMcpAuthGate? authGate, JsonRpcRequest request)
  {
    if (ValidateHeaders(context, request) is { } headerError)
    {
      await WriteSingleErrorAsync(context, request.Id, headerError).ConfigureAwait(false);
      return;
    }

    AuthInfo? authInfo = null;
    if (authGate is not null)
    {
      var gate = await authGate.AuthorizeAsync(context).ConfigureAwait(false);
      if (!gate.Authorized)
      {
        context.Response.StatusCode = gate.ChallengeStatus;
        if (gate.WwwAuthenticate is not null) context.Response.Headers.WWWAuthenticate = gate.WwwAuthenticate;
        return;
      }
      authInfo = gate.Identity;
    }

    // §10: a subscriptions/listen request opens a long-lived stream rather than producing one response.
    if (request.Method == McpMethods.SubscriptionsListen
      && handler is IMcpSubscriptionHandler subscriptionHandler
      && subscriptionHandler.SupportsSubscriptions)
    {
      await HandleSubscriptionAsync(context, subscriptionHandler, request).ConfigureAwait(false);
      return;
    }

    // §9.6: choose the response shape. When the caller supplied a progress token we commit to an event
    // stream up front so progress is delivered live; otherwise we buffer and pick the shape from whether
    // any request-scoped notification was emitted.
    var wantsStream = request.Params?["_meta"]?[MetaKeys.ProgressToken] is not null;

    if (wantsStream)
    {
      await WriteEventStreamAsync(context, handler, request, authInfo).ConfigureAwait(false);
      return;
    }

    var collected = new List<JsonRpcNotification>();
    var bufferingNotifier = new BufferingNotifier(collected);
    var response = await handler.HandleRequestAsync(request, bufferingNotifier, authInfo, context.RequestAborted).ConfigureAwait(false);

    if (collected.Count > 0)
    {
      await WriteBufferedEventStreamAsync(context, collected, response).ConfigureAwait(false);
    }
    else
    {
      await WriteSingleResponseAsync(context, response).ConfigureAwait(false);
    }
  }

  private static McpError? ValidateHeaders(HttpContext context, JsonRpcRequest request)
  {
    var headers = context.Request.Headers;

    var declaredVersion = request.Params?["_meta"]?[MetaKeys.ProtocolVersion]?.GetValue<string>();
    var headerVersion = headers["MCP-Protocol-Version"].ToString();
    if (string.IsNullOrEmpty(headerVersion))
    {
      return McpError.HeaderMismatch("Missing required MCP-Protocol-Version header (§9.3.3).");
    }
    if (declaredVersion is not null && headerVersion != declaredVersion)
    {
      return McpError.HeaderMismatch($"MCP-Protocol-Version header '{headerVersion}' does not match the body protocol version '{declaredVersion}' (§9.3.3).");
    }

    var headerMethod = headers["Mcp-Method"].ToString();
    if (string.IsNullOrEmpty(headerMethod))
    {
      return McpError.HeaderMismatch("Missing required Mcp-Method header (§9.4.1).");
    }
    if (headerMethod != request.Method)
    {
      return McpError.HeaderMismatch($"Mcp-Method header '{headerMethod}' does not match the body method '{request.Method}' (§9.4.1).");
    }

    // §9.4.2: Mcp-Name is REQUIRED on tools/call, prompts/get, and resources/read.
    var expectedName = request.Method switch
    {
      McpMethods.ToolsCall or McpMethods.PromptsGet => request.Params?["name"]?.GetValue<string>(),
      McpMethods.ResourcesRead => request.Params?["uri"]?.GetValue<string>(),
      _ => null,
    };
    if (expectedName is not null)
    {
      var headerName = headers["Mcp-Name"].ToString();
      if (string.IsNullOrEmpty(headerName))
      {
        return McpError.HeaderMismatch($"Missing required Mcp-Name header for {request.Method} (§9.4.2).");
      }
      if (headerName != expectedName)
      {
        return McpError.HeaderMismatch($"Mcp-Name header '{headerName}' does not match the body value '{expectedName}' (§9.4.2).");
      }
    }

    return null;
  }

  private static async Task WriteSingleResponseAsync(HttpContext context, JsonRpcMessage response)
  {
    context.Response.StatusCode = StatusForResponse(response);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonRpcMessageSerializer.Serialize(response), context.RequestAborted).ConfigureAwait(false);
  }

  private static async Task WriteSingleErrorAsync(HttpContext context, RequestId? id, McpError error)
  {
    var response = new JsonRpcErrorResponse(id, error.ToJsonRpcError());
    await WriteSingleResponseAsync(context, response).ConfigureAwait(false);
  }

  private static async Task WriteBufferedEventStreamAsync(HttpContext context, IReadOnlyList<JsonRpcNotification> notifications, JsonRpcMessage finalResponse)
  {
    PrepareEventStream(context);
    foreach (var notification in notifications)
    {
      await WriteEventAsync(context, notification).ConfigureAwait(false);
    }
    await WriteEventAsync(context, finalResponse).ConfigureAwait(false);
  }

  private static async Task WriteEventStreamAsync(HttpContext context, IMcpRequestHandler handler, JsonRpcRequest request, AuthInfo? authInfo)
  {
    PrepareEventStream(context);
    var notifier = new StreamingNotifier(context);
    var response = await handler.HandleRequestAsync(request, notifier, authInfo, context.RequestAborted).ConfigureAwait(false);
    await WriteEventAsync(context, response).ConfigureAwait(false);
  }

  private static async Task HandleSubscriptionAsync(HttpContext context, IMcpSubscriptionHandler handler, JsonRpcRequest request)
  {
    var requested = request.Params?["notifications"]?.Deserialize<SubscriptionFilter>(McpJson.Options) ?? new SubscriptionFilter();
    var subscriptionId = request.Id.ToString();
    var channel = Channel.CreateUnbounded<JsonRpcNotification>();

    var (honored, teardown) = handler.OpenSubscription(requested, subscriptionId, notification =>
    {
      channel.Writer.TryWrite(notification);
      return Task.CompletedTask;
    });

    PrepareEventStream(context);

    // §10.3: the first message on the stream MUST be the acknowledgement, carrying the honored filter
    // and the subscription id in _meta (§10.4).
    var ackParams = new JsonObject
    {
      ["_meta"] = new JsonObject { [MetaKeys.SubscriptionId] = subscriptionId },
      ["notifications"] = JsonSerializer.SerializeToNode(honored, McpJson.Options),
    };
    await WriteEventAsync(context, new JsonRpcNotification(McpMethods.NotificationsSubscriptionsAcknowledged, ackParams)).ConfigureAwait(false);

    try
    {
      await foreach (var notification in channel.Reader.ReadAllAsync(context.RequestAborted).ConfigureAwait(false))
      {
        await WriteEventAsync(context, notification).ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException)
    {
      // The client closed the stream (§10.7); fall through to teardown.
    }
    finally
    {
      teardown.Dispose();
    }
  }

  private static void PrepareEventStream(HttpContext context)
  {
    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    // §9.6.2: ask reverse proxies not to buffer the stream.
    context.Response.Headers["X-Accel-Buffering"] = "no";
  }

  private static async Task WriteEventAsync(HttpContext context, JsonRpcMessage message)
  {
    var data = JsonRpcMessageSerializer.Serialize(message);
    await context.Response.WriteAsync($"data: {data}\n\n", context.RequestAborted).ConfigureAwait(false);
    await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
  }

  private static int StatusForResponse(JsonRpcMessage response) => response switch
  {
    JsonRpcErrorResponse error => error.Error.Code switch
    {
      ErrorCodes.MethodNotFound => StatusCodes.Status404NotFound,
      ErrorCodes.HeaderMismatch or ErrorCodes.MissingRequiredClientCapability or ErrorCodes.UnsupportedProtocolVersion
        or ErrorCodes.InvalidParams or ErrorCodes.ParseError or ErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
      // §9.6.1 permits an error response in the single-JSON 200 shape; used for internal errors.
      _ => StatusCodes.Status200OK,
    },
    _ => StatusCodes.Status200OK,
  };

  /// <summary>Collects request-scoped notifications so the adapter can pick the response shape after the handler runs.</summary>
  private sealed class BufferingNotifier(List<JsonRpcNotification> sink) : IServerNotifier
  {
    public Task NotifyAsync(JsonRpcNotification notification)
    {
      sink.Add(notification);
      return Task.CompletedTask;
    }
  }

  /// <summary>Writes each request-scoped notification straight to the live event stream.</summary>
  private sealed class StreamingNotifier(HttpContext context) : IServerNotifier
  {
    public Task NotifyAsync(JsonRpcNotification notification) => WriteEventAsync(context, notification);
  }
}
