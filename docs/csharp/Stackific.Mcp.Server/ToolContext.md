# ToolContext class

The context handed to a [`ToolHandler`](./ToolHandler.md) for a single `tools/call` (spec §16.5). It exposes the call arguments, the inbound request metadata (including trace context, §15.4), the authenticated identity (§23), a cooperative cancellation signal (§9.6.2/§15.2), and sinks for request-scoped notifications (progress §15.1, logging §15.3) and subscription fan-out (§10).

```csharp
public sealed class ToolContext
```

## Public Members

| name | description |
| --- | --- |
| [Arguments](ToolContext/Arguments.md) { get; } | The tool arguments object (an empty object when the call omitted `arguments`, §16.5). |
| [AuthInfo](ToolContext/AuthInfo.md) { get; } | The validated bearer identity, when the request was authenticated (§23); otherwise `null`. |
| [Meta](ToolContext/Meta.md) { get; } | The inbound request `_meta` beyond the protocol-defined keys (trace context, progress token, third-party), or `null`. |
| [ProgressToken](ToolContext/ProgressToken.md) { get; } | The progress token the caller supplied in request `_meta`, or `null` if none (§15.1.2). |
| [RequestMeta](ToolContext/RequestMeta.md) { get; } | The validated per-request `_meta` envelope (protocol version, client info/capabilities, §4.3). |
| [Signal](ToolContext/Signal.md) { get; } | A cooperative cancellation signal: set when the client cancels or closes the request stream (§9.6.2/§15.2). |
| [Tasks](ToolContext/Tasks.md) { get; } | The task store for a task-augmented call (spec §25), or `null` for an ordinary call. |
| [TaskTtlMs](ToolContext/TaskTtlMs.md) { get; } | The requested task lifetime in milliseconds, when the caller supplied one (§25.4); otherwise `null`. |
| [CreateMessageAsync](ToolContext/CreateMessageAsync.md)(…) | Asks the client to run a model completion via sampling (spec §21), through the multi-round-trip mechanism (§11). Requires the client to have declared the (deprecated) `sampling` capability, else `-32003` (§11.5). |
| [ElicitInputAsync](ToolContext/ElicitInputAsync.md)(…) | Requests structured or out-of-band user input via elicitation (spec §20), using the multi-round-trip mechanism (§11): on the first round this signals `input_required` and the runtime suspends the call; when the client retries with the answer, this returns it. Requires the client to have declared the `elicitation` capability, else `-32003` (§11.5). |
| [GetBool](ToolContext/GetBool.md)(…) | Reads an optional boolean argument, returning *fallback* when absent. |
| [GetDouble](ToolContext/GetDouble.md)(…) | Reads a required numeric argument, throwing `-32602` if absent or not a number. |
| [GetInt](ToolContext/GetInt.md)(…) | Reads an optional integer argument, returning *fallback* when absent. |
| [GetString](ToolContext/GetString.md)(…) | Reads a required string argument, throwing `-32602` if absent or not a string. (2 methods) |
| [ListRootsAsync](ToolContext/ListRootsAsync.md)() | Asks the client for its filesystem roots (spec §21), through the multi-round-trip mechanism (§11). Requires the client to have declared the (deprecated) `roots` capability, else `-32003` (§11.5). |
| [LogAsync](ToolContext/LogAsync.md)(…) | Emits a `notifications/message` log entry on this request's stream, gated by the PER-REQUEST opt-in (spec §15.3.3). The originating request opts in by carrying the reserved `io.modelcontextprotocol/logLevel` key in its `_meta` (§4.3): when that key is ABSENT the server MUST NOT emit ANY log notification for the request (R-15.3.3 first bullet), and when present the server MUST NOT emit messages below the opted-in severity (R-15.3.3 second bullet). The legacy server-wide minimum (`logging/setLevel`; default `info`) is applied as an ADDITIONAL floor — the server MAY emit only a subset of the opted-in levels — so a message must clear BOTH the per-request level and the server-wide level to be sent. A dropped message is a silent no-op. Deprecated mechanism (§15.3), retained for interoperability. |
| [NotifyAsync](ToolContext/NotifyAsync.md)(…) | Emits an arbitrary request-scoped notification on this request's stream (§9.6.2). |
| [NotifySubscribersAsync](ToolContext/NotifySubscribersAsync.md)(…) | Fans a change notification out to every active subscription stream whose filter opted into its kind (spec §10.5). Used by tools that mutate server state (for example to drive the Subscriptions view). Delivery to request-scoped streams is via [`NotifyAsync`](./ToolContext/NotifyAsync.md) instead (§10.6). |
| [ReportProgressAsync](ToolContext/ReportProgressAsync.md)(…) | Emits a `notifications/progress` update for this request (spec §15.1), enforcing strict monotonicity through a per-call [`ProgressTracker`](../Stackific.Mcp.Protocol/ProgressTracker.md). No-op when the caller did not supply a [`ProgressToken`](./ToolContext/ProgressToken.md) (progress is correlated by that token), or when *progress* does not strictly exceed the last emitted value — a non-increasing update MUST NOT be sent (R-15.1.3-e), so it is dropped rather than corrupting the progress sequence. |
| [SetCacheHints](ToolContext/SetCacheHints.md)(…) | Marks this tool's result as cacheable with the given hints (spec §13). |

## See Also

* namespace [Stackific.Mcp.Server](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
