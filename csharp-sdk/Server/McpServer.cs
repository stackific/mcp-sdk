using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Stackific.Mcp.Json;
using Stackific.Mcp.JsonRpc;
using Stackific.Mcp.Protocol;
using Stackific.Mcp.Transport;

namespace Stackific.Mcp.Server;

/// <summary>
/// A spec-conformant MCP server runtime (spec §5, §16–§19): a dispatcher onto which features are
/// registered (tools, resources, resource templates, prompts, completion) and which turns a parsed
/// JSON-RPC request into the correct response. It owns no transport — the in-memory bridge and the
/// Streamable HTTP adapter call <see cref="HandleRequestAsync"/> — so the same server runs unchanged
/// over any binding. Processing is stateless: every request is validated and served on its own (§4.4).
/// </summary>
public sealed class McpServer : IMcpRequestHandler, IMcpSubscriptionHandler
{
  private readonly Implementation _serverInfo;
  private readonly ServerCapabilities _capabilities;
  private readonly string? _instructions;

  private readonly List<Tool> _toolList = [];
  private readonly Dictionary<string, ToolHandler> _toolHandlers = new(StringComparer.Ordinal);
  private readonly List<Resource> _resourceList = [];
  private readonly Dictionary<string, ResourceReadHandler> _resourceReaders = new(StringComparer.Ordinal);
  private readonly List<RegisteredTemplate> _templates = [];
  private readonly List<Prompt> _promptList = [];
  private readonly Dictionary<string, RegisteredPrompt> _prompts = new(StringComparer.Ordinal);

  private readonly Dictionary<string, Func<ToolContext, Task<McpTask>>> _taskTools = new(StringComparer.Ordinal);
  private readonly SubscriptionManager _subscriptions = new();
  private Func<JsonRpcNotification, Task> _subscriberSink;
  private InMemoryTaskStore _taskStore = new();

  /// <summary>Creates a server with the given identity, advertised capabilities, and optional instructions.</summary>
  /// <param name="serverInfo">The server identity returned in discovery (§5.3.2).</param>
  /// <param name="capabilities">The capabilities the server advertises (§6.3). Gating is enforced against these.</param>
  /// <param name="instructions">Optional natural-language guidance returned in discovery (§5.3.2).</param>
  public McpServer(Implementation serverInfo, ServerCapabilities capabilities, string? instructions = null)
  {
    _serverInfo = serverInfo;
    _capabilities = capabilities;
    _instructions = instructions;
    _subscriberSink = _subscriptions.FanOutAsync;
  }

  /// <summary>The number of items returned per page from a list operation (§12). Defaults to 50.</summary>
  public int PageSize { get; init; } = 50;

  /// <summary>The advertised server capabilities (§6.3).</summary>
  public ServerCapabilities Capabilities => _capabilities;

  /// <summary>
  /// Installs the sink used by <see cref="ToolContext.NotifySubscribersAsync"/> to fan change
  /// notifications out to active subscription streams (§10). Wired by the subscription transport.
  /// </summary>
  /// <param name="sink">The fan-out delegate.</param>
  public void SetSubscriberSink(Func<JsonRpcNotification, Task> sink) => _subscriberSink = sink;

  /// <inheritdoc/>
  public bool SupportsSubscriptions => true;

  /// <inheritdoc/>
  public (SubscriptionFilter Honored, IDisposable Teardown) OpenSubscription(
    SubscriptionFilter requested,
    string subscriptionId,
    Func<JsonRpcNotification, Task> deliver) =>
    _subscriptions.Register(requested, _capabilities, subscriptionId, deliver);

  /// <summary>The durable task store backing the Tasks extension (spec §25).</summary>
  public InMemoryTaskStore TaskStore => _taskStore;

  /// <summary>Replaces the task store (for example to inject a deterministic clock in tests).</summary>
  /// <param name="store">The store to use.</param>
  public void SetTaskStore(InMemoryTaskStore store) => _taskStore = store;

  /// <summary>
  /// Registers a task-augmented tool (spec §25.3): when a caller that declared the Tasks extension
  /// invokes it, the server returns a task handle immediately and the handler drives the work to the
  /// <see cref="TaskStore"/> in the background. The handler creates the task (via
  /// <see cref="ToolContext.Tasks"/>), starts its background work, and returns the handle.
  /// </summary>
  /// <param name="tool">The tool definition.</param>
  /// <param name="handler">The handler that creates and returns the task handle.</param>
  public void RegisterTaskTool(Tool tool, Func<ToolContext, Task<McpTask>> handler)
  {
    if (!_taskTools.TryAdd(tool.Name, handler))
    {
      throw new InvalidOperationException($"A tool named \"{tool.Name}\" is already registered.");
    }
    _toolList.Add(tool);
  }

  // ───────────────────────── Registration ─────────────────────────

  /// <summary>Registers a tool and its handler (spec §16). Names must be unique within the server.</summary>
  /// <param name="tool">The tool definition (name, schemas, annotations).</param>
  /// <param name="handler">The invocation handler.</param>
  public void RegisterTool(Tool tool, ToolHandler handler)
  {
    if (!_toolHandlers.TryAdd(tool.Name, handler))
    {
      throw new InvalidOperationException($"A tool named \"{tool.Name}\" is already registered.");
    }
    _toolList.Add(tool);
  }

  /// <summary>Registers a concrete resource and its reader (spec §17).</summary>
  /// <param name="resource">The resource descriptor.</param>
  /// <param name="reader">The reader invoked for <c>resources/read</c> of this URI.</param>
  public void RegisterResource(Resource resource, ResourceReadHandler reader)
  {
    if (!_resourceReaders.TryAdd(resource.Uri, reader))
    {
      throw new InvalidOperationException($"A resource with URI \"{resource.Uri}\" is already registered.");
    }
    _resourceList.Add(resource);
  }

  /// <summary>Registers a resource template, its reader, and optional per-variable completers (spec §17/§19).</summary>
  /// <param name="template">The resource template descriptor.</param>
  /// <param name="reader">The reader invoked when a read URI matches this template.</param>
  /// <param name="completers">Optional completion providers keyed by template-variable name.</param>
  public void RegisterResourceTemplate(
    ResourceTemplate template,
    ResourceTemplateReadHandler reader,
    IReadOnlyDictionary<string, ArgumentCompleter>? completers = null)
  {
    _templates.Add(new RegisteredTemplate(template, reader, BuildTemplateRegex(template.UriTemplate), completers));
  }

  /// <summary>Registers a prompt, its handler, and optional per-argument completers (spec §18/§19).</summary>
  /// <param name="prompt">The prompt descriptor (name, arguments).</param>
  /// <param name="handler">The handler that resolves the prompt into messages.</param>
  /// <param name="completers">Optional completion providers keyed by argument name.</param>
  public void RegisterPrompt(
    Prompt prompt,
    PromptGetHandler handler,
    IReadOnlyDictionary<string, ArgumentCompleter>? completers = null)
  {
    if (!_prompts.TryAdd(prompt.Name, new RegisteredPrompt(prompt, handler, completers)))
    {
      throw new InvalidOperationException($"A prompt named \"{prompt.Name}\" is already registered.");
    }
    _promptList.Add(prompt);
  }

  // ───────────────────────── Dispatch ─────────────────────────

  /// <inheritdoc/>
  public async Task<JsonRpcMessage> HandleRequestAsync(
    JsonRpcRequest request,
    IServerNotifier notifier,
    AuthInfo? authInfo,
    CancellationToken cancellationToken)
  {
    try
    {
      var meta = RequestMeta.Parse(request.Params);
      if (!ProtocolRevision.IsSupported(meta.ProtocolVersion))
      {
        throw McpError.UnsupportedProtocolVersion(ProtocolRevision.Supported, meta.ProtocolVersion);
      }

      var result = await DispatchAsync(request, meta, notifier, authInfo, cancellationToken).ConfigureAwait(false);
      return new JsonRpcSuccessResponse(request.Id, result);
    }
    catch (McpError error)
    {
      return new JsonRpcErrorResponse(request.Id, error.ToJsonRpcError());
    }
    catch (OperationCanceledException)
    {
      // The client closed the stream / cancelled (§9.6.2). Surface a benign error response.
      return new JsonRpcErrorResponse(request.Id, McpError.InternalError("Request cancelled.").ToJsonRpcError());
    }
    catch (Exception error)
    {
      return new JsonRpcErrorResponse(request.Id, McpError.InternalError(error.Message).ToJsonRpcError());
    }
  }

  /// <inheritdoc/>
  public Task HandleNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
  {
    // The client may send notifications/cancelled; cancellation is wired by the transport via the
    // CancellationToken on the originating request, so unrecognized notifications are simply ignored (§3.4).
    return Task.CompletedTask;
  }

  private Task<JsonObject> DispatchAsync(
    JsonRpcRequest request,
    RequestMeta meta,
    IServerNotifier notifier,
    AuthInfo? authInfo,
    CancellationToken cancellationToken) => request.Method switch
    {
      McpMethods.Discover => Task.FromResult(Discover()),
      McpMethods.Ping => Task.FromResult(Complete(new JsonObject())),
      McpMethods.ToolsList => Task.FromResult(ListTools(request.Params)),
      McpMethods.ToolsCall => CallToolAsync(request, meta, notifier, authInfo, cancellationToken),
      McpMethods.ResourcesList => Task.FromResult(ListResources(request.Params)),
      McpMethods.ResourceTemplatesList => Task.FromResult(ListResourceTemplates(request.Params)),
      McpMethods.ResourcesRead => ReadResourceAsync(request.Params),
      McpMethods.PromptsList => Task.FromResult(ListPrompts(request.Params)),
      McpMethods.PromptsGet => GetPromptAsync(request.Params),
      McpMethods.CompletionComplete => Task.FromResult(RunCompletion(request.Params)),
      McpMethods.TasksGet => Task.FromResult(GetTask(request.Params, meta)),
      McpMethods.TasksCancel => Task.FromResult(CancelTask(request.Params, meta)),
      McpMethods.TasksUpdate => Task.FromResult(UpdateTask(request.Params, meta)),
      _ => throw McpError.MethodNotFound(request.Method),
    };

  private JsonObject Discover()
  {
    var result = new DiscoverResult
    {
      SupportedVersions = ProtocolRevision.Supported,
      Capabilities = _capabilities,
      ServerInfo = _serverInfo,
      Instructions = _instructions,
    };
    return Complete(Serialize(result));
  }

  private JsonObject ListTools(JsonObject? prms)
  {
    RequireCapability(_capabilities.Tools is not null, McpMethods.ToolsList);
    var (page, nextCursor) = Paginate(_toolList, prms);
    var result = Serialize(new ListToolsResult { Tools = page, NextCursor = nextCursor, TtlMs = ListTtlMs, CacheScope = CacheScope.Public });
    return Complete(result);
  }

  private JsonObject ListResources(JsonObject? prms)
  {
    RequireCapability(_capabilities.Resources is not null, McpMethods.ResourcesList);
    var (page, nextCursor) = Paginate(_resourceList, prms);
    var result = Serialize(new ListResourcesResult { Resources = page, NextCursor = nextCursor, TtlMs = ListTtlMs, CacheScope = CacheScope.Public });
    return Complete(result);
  }

  private JsonObject ListResourceTemplates(JsonObject? prms)
  {
    RequireCapability(_capabilities.Resources is not null, McpMethods.ResourceTemplatesList);
    var all = _templates.Select(t => t.Template).ToList();
    var (page, nextCursor) = Paginate(all, prms);
    var result = Serialize(new ListResourceTemplatesResult { ResourceTemplates = page, NextCursor = nextCursor, TtlMs = ListTtlMs, CacheScope = CacheScope.Public });
    return Complete(result);
  }

  private JsonObject ListPrompts(JsonObject? prms)
  {
    RequireCapability(_capabilities.Prompts is not null, McpMethods.PromptsList);
    var (page, nextCursor) = Paginate(_promptList, prms);
    var result = Serialize(new ListPromptsResult { Prompts = page, NextCursor = nextCursor, TtlMs = ListTtlMs, CacheScope = CacheScope.Public });
    return Complete(result);
  }

  private async Task<JsonObject> CallToolAsync(
    JsonRpcRequest request,
    RequestMeta meta,
    IServerNotifier notifier,
    AuthInfo? authInfo,
    CancellationToken cancellationToken)
  {
    RequireCapability(_capabilities.Tools is not null, McpMethods.ToolsCall);
    var prms = request.Params ?? throw McpError.InvalidParams("tools/call requires params.");
    var name = RequireStringParam(prms, "name");
    var tool = _toolList.FirstOrDefault(t => t.Name == name)
      ?? throw McpError.InvalidParams($"Unknown tool: {name}", new JsonObject { ["toolName"] = name });
    var arguments = prms["arguments"] as JsonObject ?? new JsonObject();
    SchemaValidation.ValidateArguments(tool.InputSchema, arguments, name);

    var progressToken = ReadProgressToken(meta);
    var inputResponses = ReadInputResponses(prms);

    // §25.3: a task-augmented tool returns a task handle when the caller declared the Tasks extension.
    if (_taskTools.TryGetValue(name, out var taskHandler))
    {
      RequireTasksCapability(meta);
      const long defaultTtlMs = 300000;
      var taskContext = new ToolContext(arguments, meta, authInfo, progressToken, notifier, _subscriberSink, inputResponses, cancellationToken, _taskStore, defaultTtlMs);
      var task = await taskHandler(taskContext).ConfigureAwait(false);
      var createResult = Serialize(new CreateTaskResult
      {
        TaskId = task.TaskId,
        Status = task.Status,
        StatusMessage = task.StatusMessage,
        CreatedAt = task.CreatedAt,
        LastUpdatedAt = task.LastUpdatedAt,
        TtlMs = task.TtlMs,
        PollIntervalMs = task.PollIntervalMs,
      });
      createResult["resultType"] = ResultTypes.Task;
      return createResult;
    }

    var handler = _toolHandlers[name];
    var context = new ToolContext(arguments, meta, authInfo, progressToken, notifier, _subscriberSink, inputResponses, cancellationToken);

    try
    {
      var result = await handler(context).ConfigureAwait(false);
      var resultObject = Serialize(result);
      if (context.CacheHints is { } hints)
      {
        resultObject["ttlMs"] = hints.TtlMs;
        resultObject["cacheScope"] = hints.CacheScope == CacheScope.Public ? "public" : "private";
      }
      return Complete(resultObject);
    }
    catch (InputRequiredSignal signal)
    {
      // §11.2: the tool needs client input. Suspend the call by returning an input_required result.
      var inputRequired = Serialize(new InputRequiredResult
      {
        InputRequests = new Dictionary<string, InputRequest> { [signal.Key] = signal.Request },
      });
      inputRequired["resultType"] = ResultTypes.InputRequired;
      return inputRequired;
    }
  }

  private static IReadOnlyDictionary<string, JsonNode>? ReadInputResponses(JsonObject prms)
  {
    if (prms["inputResponses"] is not JsonObject responses) return null;
    var map = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
    foreach (var (key, value) in responses)
    {
      if (value is not null) map[key] = value;
    }
    return map.Count > 0 ? map : null;
  }

  private async Task<JsonObject> ReadResourceAsync(JsonObject? prms)
  {
    RequireCapability(_capabilities.Resources is not null, McpMethods.ResourcesRead);
    if (prms is null) throw McpError.InvalidParams("resources/read requires params.");
    var uri = RequireStringParam(prms, "uri");

    if (_resourceReaders.TryGetValue(uri, out var reader))
    {
      var result = await reader(uri).ConfigureAwait(false);
      return Complete(WithReadCacheHints(Serialize(result)));
    }

    foreach (var template in _templates)
    {
      var match = template.Pattern.Match(uri);
      if (!match.Success) continue;
      var variables = template.Pattern.GetGroupNames()
        .Where(n => !int.TryParse(n, out _))
        .ToDictionary(n => n, n => match.Groups[n].Value, StringComparer.Ordinal);
      var result = await template.Reader(uri, variables).ConfigureAwait(false);
      return Complete(WithReadCacheHints(Serialize(result)));
    }

    throw McpError.InvalidParams("Resource not found", new JsonObject { ["uri"] = uri });
  }

  private async Task<JsonObject> GetPromptAsync(JsonObject? prms)
  {
    RequireCapability(_capabilities.Prompts is not null, McpMethods.PromptsGet);
    if (prms is null) throw McpError.InvalidParams("prompts/get requires params.");
    var name = RequireStringParam(prms, "name");
    if (!_prompts.TryGetValue(name, out var registered))
    {
      throw McpError.InvalidParams($"Unknown prompt: {name}");
    }

    var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
    if (prms["arguments"] is JsonObject argsObject)
    {
      foreach (var (key, value) in argsObject)
      {
        if (value is JsonValue v && v.GetValueKind() == JsonValueKind.String) arguments[key] = v.GetValue<string>();
      }
    }

    foreach (var argument in registered.Prompt.Arguments ?? [])
    {
      if (argument.Required == true && !arguments.ContainsKey(argument.Name))
      {
        throw McpError.InvalidParams($"Missing required prompt argument: {argument.Name}");
      }
    }

    var result = await registered.Handler(arguments).ConfigureAwait(false);
    return Complete(Serialize(result));
  }

  private JsonObject RunCompletion(JsonObject? prms)
  {
    RequireCapability(_capabilities.Completions is not null, McpMethods.CompletionComplete);
    if (prms is null) throw McpError.InvalidParams("completion/complete requires params.");

    var request = prms.Deserialize<CompleteRequestParams>(McpJson.Options)
      ?? throw McpError.InvalidParams("Invalid completion/complete params.");
    var value = request.Argument.Value;

    IReadOnlyList<string> candidates = request.Ref switch
    {
      PromptReference promptRef => CompletePrompt(promptRef.Name, request.Argument.Name, value),
      ResourceTemplateReference templateRef => CompleteTemplate(templateRef.Uri, request.Argument.Name, value),
      _ => throw McpError.InvalidParams("Unknown completion reference type."),
    };

    var capped = candidates.Count > 100 ? candidates.Take(100).ToList() : candidates;
    var result = new CompleteResult
    {
      Completion = new CompletionValues { Values = capped, Total = candidates.Count, HasMore = candidates.Count > 100 },
    };
    return Complete(Serialize(result));
  }

  private IReadOnlyList<string> CompletePrompt(string promptName, string argument, string value)
  {
    if (!_prompts.TryGetValue(promptName, out var registered))
    {
      throw McpError.InvalidParams($"Unknown prompt: {promptName}");
    }
    if (registered.Completers is not null && registered.Completers.TryGetValue(argument, out var completer))
    {
      return completer(value);
    }
    return [];
  }

  private IReadOnlyList<string> CompleteTemplate(string uri, string variable, string value)
  {
    foreach (var template in _templates)
    {
      if (template.Template.UriTemplate != uri) continue;
      if (template.Completers is not null && template.Completers.TryGetValue(variable, out var completer))
      {
        return completer(value);
      }
      return [];
    }
    throw McpError.InvalidParams($"Unknown resource template: {uri}");
  }

  private JsonObject GetTask(JsonObject? prms, RequestMeta meta)
  {
    RequireCapability(_capabilities.HasExtension(MetaKeys.TasksExtension), McpMethods.TasksGet);
    RequireTasksCapability(meta);
    var taskId = RequireStringParam(prms ?? throw McpError.InvalidParams("tasks/get requires params."), "taskId");
    return Complete(Serialize(_taskStore.Get(taskId))); // Get throws -32602 for unknown ids (§25.7)
  }

  private JsonObject CancelTask(JsonObject? prms, RequestMeta meta)
  {
    RequireCapability(_capabilities.HasExtension(MetaKeys.TasksExtension), McpMethods.TasksCancel);
    RequireTasksCapability(meta);
    var taskId = RequireStringParam(prms ?? throw McpError.InvalidParams("tasks/cancel requires params."), "taskId");
    _taskStore.Cancel(taskId);
    return Complete(new JsonObject());
  }

  private JsonObject UpdateTask(JsonObject? prms, RequestMeta meta)
  {
    RequireCapability(_capabilities.HasExtension(MetaKeys.TasksExtension), McpMethods.TasksUpdate);
    RequireTasksCapability(meta);
    _ = RequireStringParam(prms ?? throw McpError.InvalidParams("tasks/update requires params."), "taskId");
    // This SDK's reference tasks do not request mid-flight input; the acknowledgement is empty (§25.8).
    return Complete(new JsonObject());
  }

  private static void RequireTasksCapability(RequestMeta meta)
  {
    if (!meta.ClientCapabilities.HasExtension(MetaKeys.TasksExtension))
    {
      throw McpError.MissingRequiredClientCapability(
        new JsonObject { ["extensions"] = new JsonObject { [MetaKeys.TasksExtension] = new JsonObject() } });
    }
  }

  // ───────────────────────── Helpers ─────────────────────────

  /// <summary>The TTL hint (ms) applied to cacheable list results (§13); zero means re-fetch each time.</summary>
  private const long ListTtlMs = 0;

  private static JsonObject Complete(JsonObject result)
  {
    result["resultType"] = ResultTypes.Complete;
    return result;
  }

  private static JsonObject WithReadCacheHints(JsonObject result)
  {
    result["ttlMs"] ??= 0;
    result["cacheScope"] ??= "public";
    return result;
  }

  private static JsonObject Serialize<T>(T value) => JsonSerializer.SerializeToNode(value, McpJson.Options)!.AsObject();

  private static void RequireCapability(bool declared, string method)
  {
    if (!declared) throw McpError.MethodNotFound(method);
  }

  private static string RequireStringParam(JsonObject prms, string field) =>
    prms[field] is JsonValue v && v.GetValueKind() == JsonValueKind.String
      ? v.GetValue<string>()
      : throw McpError.InvalidParams($"Required parameter \"{field}\" is missing or not a string.");

  private static ProgressToken? ReadProgressToken(RequestMeta meta)
  {
    if (meta.Additional?[MetaKeys.ProgressToken] is JsonNode node)
    {
      return ProgressToken.FromJsonNode(node);
    }
    return null;
  }

  private (IReadOnlyList<T> Page, string? NextCursor) Paginate<T>(IReadOnlyList<T> all, JsonObject? prms)
  {
    var offset = 0;
    if (prms?["cursor"] is JsonValue cursorValue && cursorValue.GetValueKind() == JsonValueKind.String)
    {
      var raw = cursorValue.GetValue<string>();
      try
      {
        offset = int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(raw)), System.Globalization.CultureInfo.InvariantCulture);
      }
      catch
      {
        throw McpError.InvalidParams("Invalid or expired pagination cursor.");
      }
      if (offset < 0 || offset > all.Count) throw McpError.InvalidParams("Invalid or expired pagination cursor.");
    }

    var page = all.Skip(offset).Take(PageSize).ToList();
    var nextOffset = offset + page.Count;
    var nextCursor = nextOffset < all.Count
      ? Convert.ToBase64String(Encoding.UTF8.GetBytes(nextOffset.ToString(System.Globalization.CultureInfo.InvariantCulture)))
      : null;
    return (page, nextCursor);
  }

  private static Regex BuildTemplateRegex(string uriTemplate)
  {
    var builder = new StringBuilder("^");
    var i = 0;
    while (i < uriTemplate.Length)
    {
      if (uriTemplate[i] == '{')
      {
        var end = uriTemplate.IndexOf('}', i);
        if (end < 0) throw new ArgumentException($"Malformed URI template: {uriTemplate}");
        var name = uriTemplate[(i + 1)..end];
        builder.Append("(?<").Append(name).Append(">[^/]+)");
        i = end + 1;
      }
      else
      {
        builder.Append(Regex.Escape(uriTemplate[i].ToString()));
        i++;
      }
    }
    builder.Append('$');
    return new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
  }

  private sealed record RegisteredTemplate(
    ResourceTemplate Template,
    ResourceTemplateReadHandler Reader,
    Regex Pattern,
    IReadOnlyDictionary<string, ArgumentCompleter>? Completers);

  private sealed record RegisteredPrompt(
    Prompt Prompt,
    PromptGetHandler Handler,
    IReadOnlyDictionary<string, ArgumentCompleter>? Completers);
}
