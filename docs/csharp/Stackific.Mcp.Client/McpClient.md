# McpClient class

The MCP client host (spec §4, §5): it stamps every request with the required per-request `_meta` envelope (protocol revision, client identity, client capabilities), correlates each response to its request, performs discovery and revision selection, and exposes typed convenience methods for the server features. It is transport-agnostic — give it any [`ClientTransport`](../Stackific.Mcp.Transport/ClientTransport.md).

```csharp
public sealed class McpClient : IAsyncDisposable
```

## Public Members

| name | description |
| --- | --- |
| [McpClient](McpClient/McpClient.md)(…) | Creates a client over *transport* with the given identity and capabilities. |
| [ActiveSubscriptionIds](McpClient/ActiveSubscriptionIds.md) { get; } | The ids of the client's currently-active subscriptions (spec §10.7). |
| [ClientCapabilities](McpClient/ClientCapabilities.md) { get; } | The capabilities this client advertises on every request. |
| [IsConnected](McpClient/IsConnected.md) { get; } | Whether discovery has completed against the server. |
| [NegotiatedVersion](McpClient/NegotiatedVersion.md) { get; } | The protocol revision negotiated with the server, or `null` before discovery (§5.4). |
| [ServerCapabilities](McpClient/ServerCapabilities.md) { get; } | The server's advertised capabilities from the last discovery, or `null` (§6.3). |
| [ServerInfo](McpClient/ServerInfo.md) { get; } | The server's identity from the last discovery, or `null` (§5.3.2). |
| [Transport](McpClient/Transport.md) { get; } | The underlying transport (exposed so a host can tap the wire). |
| [CallToolAsync](McpClient/CallToolAsync.md)(…) | Invokes a tool and returns the raw result object (which may be a `CallToolResult` or an input-required result, §11/§16.5). |
| [CallToolValidatedAsync](McpClient/CallToolValidatedAsync.md)(…) | Invokes a tool and validates the returned result on receipt (spec §3.6, §16.6): a result whose `resultType` the client does not recognize is rejected (§3.6, the unrecognized-discriminator MUST), and — when the tool's `outputSchema` is known to this client from a prior [`ListToolsAsync`](./McpClient/ListToolsAsync.md) — a completed result's `structuredContent` is validated against that schema, rejecting a non-conforming server result. When no schema is known the result is returned unvalidated. Prefer this over [`CallToolAsync`](./McpClient/CallToolAsync.md) to defensively enforce the server's declared output contract. |
| [CallToolWithInputAsync](McpClient/CallToolWithInputAsync.md)(…) | Invokes a tool, fulfilling any `input_required` rounds via the registered input handlers and retrying until the call completes (spec §11/§16.5). This is the driver behind the elicitation, sampling, and roots flows. |
| [CancelTaskAsync](McpClient/CancelTaskAsync.md)(…) | Requests cancellation of a task (spec §25.9). |
| [CompleteAsync](McpClient/CompleteAsync.md)(…) | Requests argument completions (spec §19.2). |
| [CreateTaskAsync](McpClient/CreateTaskAsync.md)(…) | Invokes a tool as a task (spec §25.3): the client must have declared the Tasks extension, and the server returns a task handle (`resultType: "task"`) for an eligible tool. Poll with [`GetTaskAsync`](./McpClient/GetTaskAsync.md) or drive to completion with [`PollTaskUntilTerminalAsync`](./McpClient/PollTaskUntilTerminalAsync.md). |
| [DiscoverAsync](McpClient/DiscoverAsync.md)() | Performs `server/discover` (spec §5.3), caches the server's identity and capabilities, and selects the highest mutually supported revision for subsequent requests (§5.4). |
| [DisposeAsync](McpClient/DisposeAsync.md)() |  |
| [GetPromptAsync](McpClient/GetPromptAsync.md)(…) | Resolves a prompt with arguments (spec §18.4). |
| [GetSubscription](McpClient/GetSubscription.md)(…) | Returns the active [`Subscription`](../Stackific.Mcp.Protocol/Subscription.md) with *subscriptionId*, or `null` (spec §10.7). |
| [GetTaskAsync](McpClient/GetTaskAsync.md)(…) | Retrieves a task's current detailed state (spec §25.7). |
| [ListPromptsAsync](McpClient/ListPromptsAsync.md)(…) | Lists the server's prompts (spec §18.2). |
| [ListResourcesAsync](McpClient/ListResourcesAsync.md)(…) | Lists the server's resources (spec §17.2). |
| [ListResourceTemplatesAsync](McpClient/ListResourceTemplatesAsync.md)(…) | Lists the server's resource templates (spec §17.3). |
| [ListToolsAsync](McpClient/ListToolsAsync.md)(…) | Lists the server's tools (spec §16.2). |
| [PingAsync](McpClient/PingAsync.md)() | Sends a liveness `ping`. |
| [PollTaskUntilTerminalAsync](McpClient/PollTaskUntilTerminalAsync.md)(…) | Polls `tasks/get` until the task reaches a terminal status — `completed`, `failed`, or `cancelled` (spec §25.5, §25.7) — then returns the final detailed task object. Honors the task's recommended `pollIntervalMs` (adopting the latest observed value, §25.7) and supports an overall timeout and a cancellation signal. |
| [ReadResourceAsync](McpClient/ReadResourceAsync.md)(…) | Reads a resource by URI (spec §17.5). |
| [RegisterInputHandler](McpClient/RegisterInputHandler.md)(…) | Registers a handler for a server-initiated input request kind (spec §11): `elicitation/create` (§20), `sampling/createMessage` (§21), or `roots/list` (§21). The handler receives the input request's `params` and returns the corresponding response object (an `ElicitResult`, `CreateMessageResult`, or `ListRootsResult`). |
| [RequestAsync](McpClient/RequestAsync.md)(…) | Sends an arbitrary request with the per-request `_meta` envelope applied, returning the result object on success and throwing [`McpError`](../Stackific.Mcp.JsonRpc/McpError.md) on a JSON-RPC error (spec §22). |
| [RequestWithInputAsync](McpClient/RequestWithInputAsync.md)(…) | Sends a request and drives the multi-round-trip loop (spec §11): on an `input_required` result it fulfills each input request via the registered handlers, accumulates the responses, echoes `requestState`, and resends the original method with the original arguments until a final result. |
| [ServerSupports](McpClient/ServerSupports.md)(…) | Returns `true` if the server advertised the capability gating *check*. |
| [SubscribeAsync](McpClient/SubscribeAsync.md)(…) | Opens a subscription stream for server-initiated change notifications (spec §10): sends `subscriptions/listen`, awaits the acknowledgement, and delivers each matching change notification to *onNotification* until the returned handle is unsubscribed. |
| [UpdateTaskAsync](McpClient/UpdateTaskAsync.md)(…) | Supplies input to a task awaiting it via `tasks/update` (spec §25.8): the responses are keyed by a currently-outstanding `inputRequests` key from the task's `input_required` state. The server binds the outstanding subset, drops stale keys (R-25.8-h), and advances the task. |
| const [DefaultTaskTtlMs](McpClient/DefaultTaskTtlMs.md) | The sentinel default for [`CreateTaskAsync`](./McpClient/CreateTaskAsync.md)'s `ttlMs` meaning "use the protocol default lifetime" (§25.4). |

## See Also

* namespace [Stackific.Mcp.Client](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
