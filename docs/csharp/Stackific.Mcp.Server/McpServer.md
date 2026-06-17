# McpServer class

A spec-conformant MCP server runtime (spec §5, §16–§19): a dispatcher onto which features are registered (tools, resources, resource templates, prompts, completion) and which turns a parsed JSON-RPC request into the correct response. It owns no transport — the in-memory bridge and the Streamable HTTP adapter call [`HandleRequestAsync`](./McpServer/HandleRequestAsync.md) — so the same server runs unchanged over any binding. Processing is stateless: every request is validated and served on its own (§4.4).

```csharp
public sealed class McpServer : IMcpRequestHandler, IMcpSubscriptionHandler
```

## Public Members

| name | description |
| --- | --- |
| [McpServer](McpServer/McpServer.md)(…) | Creates a server with the given identity, advertised capabilities, and optional instructions. |
| [CacheScope](McpServer/CacheScope.md) { get; set; } | The default top-level `cacheScope` for cacheable-method results (§13.3). Defaults to Private — the privacy-default a server MUST apply when it cannot reliably distinguish authorization contexts (R-13.1-e, R-13.3-h). Emission is routed through [`ResolveCacheScope`](../Stackific.Mcp.Protocol/Caching/ResolveCacheScope.md) so the resolved scope always lands on the privacy default for any unrecognized value. |
| [CacheTtlMs](McpServer/CacheTtlMs.md) { get; set; } | The default freshness hint (ms) stamped as the top-level `ttlMs` on the five cacheable-method results (§13.4, R-13.4-b). Defaults to `0` — a non-caching server still MUST emit the field. A per-call [`SetCacheHints`](./ToolContext/SetCacheHints.md) override (the analog of TS `withCacheHints`) takes precedence on a `tools/call` result. |
| [Capabilities](McpServer/Capabilities.md) { get; } | The advertised server capabilities (§6.3). |
| [DiscoverMeta](McpServer/DiscoverMeta.md) { get; set; } | Optional result-level `_meta` attached to the `server/discover` result (§5.3.2, R-5.3.2-k): arbitrary protocol-defined or vendor metadata. `null` (the default) attaches none. |
| [MinLogLevel](McpServer/MinLogLevel.md) { get; } | The server's current minimum log-emit severity (§15.3), settable via `logging/setLevel`. |
| [PageSize](McpServer/PageSize.md) { get; set; } | The number of items returned per page from a list operation (§12). Defaults to 50. |
| [SupportsSubscriptions](McpServer/SupportsSubscriptions.md) { get; } |  |
| [TaskStore](McpServer/TaskStore.md) { get; } | The durable task store backing the Tasks extension (spec §25). |
| [GetToolInputSchema](McpServer/GetToolInputSchema.md)(…) | Resolves a registered tool's `inputSchema` by name, or `null` for an unknown tool (§16.4). The Streamable HTTP adapter consumes this so a `tools/call` can have its `Mcp-Param-*` headers validated/decoded against the tool's schema (§9.5.4) — see [`MapMcp`](../Stackific.Mcp.Transport/StreamableHttpServer/MapMcp.md). |
| [HandleNotificationAsync](McpServer/HandleNotificationAsync.md)(…) |  |
| [HandleRequestAsync](McpServer/HandleRequestAsync.md)(…) |  |
| [OpenSubscription](McpServer/OpenSubscription.md)(…) |  |
| [RegisterPrompt](McpServer/RegisterPrompt.md)(…) | Registers a prompt, its handler, and optional per-argument completers (spec §18/§19). |
| [RegisterResource](McpServer/RegisterResource.md)(…) | Registers a concrete resource and its reader (spec §17). |
| [RegisterResourceTemplate](McpServer/RegisterResourceTemplate.md)(…) | Registers a resource template, its reader, and optional per-variable completers (spec §17/§19). |
| [RegisterTaskTool](McpServer/RegisterTaskTool.md)(…) | Registers a task-augmented tool (spec §25.3): when a caller that declared the Tasks extension invokes it, the server returns a task handle immediately and the handler drives the work to the [`TaskStore`](./McpServer/TaskStore.md) in the background. The handler creates the task (via [`Tasks`](./ToolContext/Tasks.md)), starts its background work, and returns the handle. |
| [RegisterTool](McpServer/RegisterTool.md)(…) | Registers a tool and its handler (spec §16). Names must be unique within the server. |
| [SetSubscriberSink](McpServer/SetSubscriberSink.md)(…) | Installs the sink used by [`NotifySubscribersAsync`](./ToolContext/NotifySubscribersAsync.md) to fan change notifications out to active subscription streams (§10). Wired by the subscription transport. |
| [SetTaskStore](McpServer/SetTaskStore.md)(…) | Replaces the task store (for example to inject a deterministic clock in tests) and wires its status-change listener to fan a `notifications/tasks` push to subscribers (§25.10). Every observable status change — including background work driven by a task-augmented tool — is forwarded to the [`SubscriptionManager`](./SubscriptionManager.md), which delivers it only to streams that opted into the task id via a `taskIds` subscription filter. |

## See Also

* interface [IMcpRequestHandler](../Stackific.Mcp.Transport/IMcpRequestHandler.md)
* interface [IMcpSubscriptionHandler](../Stackific.Mcp.Transport/IMcpSubscriptionHandler.md)
* namespace [Stackific.Mcp.Server](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
