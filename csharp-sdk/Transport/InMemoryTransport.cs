using System.Text.Json;

using Stackific.Mcp.JsonRpc;
using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Transport;

/// <summary>
/// A client transport that bridges directly to an in-process <see cref="IMcpRequestHandler"/> (an
/// <c>McpServer</c>) with no HTTP or serialization round-trip. It mirrors the Streamable HTTP request
/// shape — each request gets its own logical response stream over which interim notifications are
/// delivered before the final response (spec §9.6) — which makes it ideal for tests and for embedding
/// a server in the same process. Frames are still offered to the wire taps.
/// </summary>
public sealed class InMemoryClientTransport : ClientTransport
{
  private readonly IMcpRequestHandler _server;
  private readonly AuthInfo? _authInfo;

  /// <summary>Creates an in-memory transport bound to <paramref name="server"/>.</summary>
  /// <param name="server">The request handler to bridge to.</param>
  /// <param name="authInfo">An optional pre-validated identity to attach to every request (§23).</param>
  public InMemoryClientTransport(IMcpRequestHandler server, AuthInfo? authInfo = null)
  {
    _server = server;
    _authInfo = authInfo;
  }

  /// <inheritdoc/>
  public override async Task<JsonRpcMessage> SendRequestAsync(JsonRpcRequest request, RequestOptions options)
  {
    TapSend(request);
    var notifier = new CallbackNotifier(notification =>
    {
      TapReceive(notification);
      options.OnNotification?.Invoke(notification);
    });

    var response = await _server
      .HandleRequestAsync(request, notifier, _authInfo, options.CancellationToken)
      .ConfigureAwait(false);

    TapReceive(response);
    return response;
  }

  /// <inheritdoc/>
  public override async Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken = default)
  {
    TapSend(notification);
    await _server.HandleNotificationAsync(notification, cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public override Task<SubscriptionHandle> OpenSubscriptionAsync(
    JsonRpcRequest listenRequest,
    Action<JsonRpcNotification> onNotification,
    CancellationToken cancellationToken = default)
  {
    TapSend(listenRequest);
    if (_server is not IMcpSubscriptionHandler subscriptionHandler || !subscriptionHandler.SupportsSubscriptions)
    {
      throw McpError.MethodNotFound(Protocol.McpMethods.SubscriptionsListen);
    }
    var requested = listenRequest.Params?["notifications"].Deserialize<SubscriptionFilter>(McpJson.Options) ?? new SubscriptionFilter();
    var (honored, teardown) = subscriptionHandler.OpenSubscription(requested, listenRequest.Id.ToString(), notification =>
    {
      TapReceive(notification);
      onNotification(notification);
      return Task.CompletedTask;
    });
    return Task.FromResult(new SubscriptionHandle
    {
      HonoredFilter = honored,
      Unsubscribe = () => { teardown.Dispose(); return ValueTask.CompletedTask; },
    });
  }

  /// <summary>An <see cref="IServerNotifier"/> that forwards each notification to a callback.</summary>
  private sealed class CallbackNotifier : IServerNotifier
  {
    private readonly Action<JsonRpcNotification> _onNotification;

    public CallbackNotifier(Action<JsonRpcNotification> onNotification) => _onNotification = onNotification;

    public Task NotifyAsync(JsonRpcNotification notification)
    {
      _onNotification(notification);
      return Task.CompletedTask;
    }
  }
}
