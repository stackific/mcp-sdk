# CapabilityNegotiation class

The per-request, stateless capability-negotiation rules that gate every optional feature (spec §6.1–§6.4) — the C# counterpart of the TypeScript `protocol/capability-negotiation.ts` module. Because MCP is stateless, a feature is usable only when BOTH peers declare the governing capability/sub-flag, and capabilities are read from the current request only — never inferred from a prior request (R-6.4-c).

```csharp
public static class CapabilityNegotiation
```

## Public Members

| name | description |
| --- | --- |
| static [DeprecatedClientCapabilities](CapabilityNegotiation/DeprecatedClientCapabilities.md) { get; } | Client capabilities marked Deprecated; new implementations SHOULD NOT rely on them (R-6.2-j, R-6.2-m). |
| static [DeprecatedServerCapabilities](CapabilityNegotiation/DeprecatedServerCapabilities.md) { get; } | Server capabilities marked Deprecated; new implementations SHOULD NOT rely on them (R-6.3-q). |
| static [NotificationRequiredCapabilityMap](CapabilityNegotiation/NotificationRequiredCapabilityMap.md) { get; } | Maps a server-to-client notification to the capability/sub-flag that gates it (§6.2, §6.3). Mirrors TS `NOTIFICATION_REQUIRED_CAPABILITY`. |
| static [ServerMethodCapability](CapabilityNegotiation/ServerMethodCapability.md) { get; } | Maps a server method to the [`ServerCapabilities`](./ServerCapabilities.md) field that gates it (§6.2, §6.3). Mirrors TS `SERVER_METHOD_CAPABILITY`. |
| const [CapabilityErrorHttpStatus](CapabilityNegotiation/CapabilityErrorHttpStatus.md) | Capability-negotiation errors ride HTTP `400 Bad Request` (R-6.4-i, R-6.4-k). |
| static [ClientDeclares](CapabilityNegotiation/ClientDeclares.md)(…) | Returns `true` when the client's raw capability map declares *capability* (§6.1). Presence of an object means supported; `elicitation.form` is the implicit baseline (true whenever `elicitation` is present, R-6.2-e), while `elicitation.url`, `sampling.context`, and `sampling.tools` require their own sub-flag object. Mirrors TS `clientDeclares`. |
| static [ClientShouldExpectNotification](CapabilityNegotiation/ClientShouldExpectNotification.md)(…) | Returns `true` when a client should expect *notification* given the server's declared capabilities. When the gating sub-flag is absent or `false`, the client MUST NOT expect the notification (R-6.3-h, R-6.3-l, R-6.3-o). Mirrors TS `clientShouldExpectNotification`. |
| static [ComputeMissingClientCapabilities](CapabilityNegotiation/ComputeMissingClientCapabilities.md)(…) | Returns the subset of *required* capabilities not present in *declared* (compared by top-level key presence; capabilities are never inferred from a prior request) (R-6.4-c, R-6.4-d, R-6.4-h). The returned object's values are deep-cloned from *required*. Mirrors TS `computeMissingClientCapabilities`. |
| static [DecideDegradation](CapabilityNegotiation/DecideDegradation.md)(…) | Decides how to handle an operation when the other peer may not declare the optional behavior it would use (R-6.4-l, R-6.4-m): Proceed when the peer declares it, Fallback when it does not but the behavior is optional, and Reject only when the missing behavior is mandatory. A peer MUST NOT reject merely because the other declared fewer capabilities (R-6.4-m). Mirrors TS `decideDegradation`. |
| static [GateRequiredClientCapabilities](CapabilityNegotiation/GateRequiredClientCapabilities.md)(…) | Gates a request against the capabilities it requires (§6.4, R-6.4-h). Returns `null` when every required capability is declared (the request is allowed); otherwise returns the `-32003``MissingRequiredClientCapability` error whose `data.requiredCapabilities` lists exactly the required-but-undeclared capabilities. Mirrors TS `gateRequiredClientCapabilities`. |
| static [HttpStatusForCapabilityError](CapabilityNegotiation/HttpStatusForCapabilityError.md)(…) | Returns `400` for the capability-negotiation error codes — `-32003` (missing required client capability) and `-32602` (malformed request omitting a required `_meta` field) — otherwise `null` (R-6.4-i, R-6.4-k). Mirrors TS `httpStatusForCapabilityError`. |
| static [IsDeprecatedClientCapability](CapabilityNegotiation/IsDeprecatedClientCapability.md)(…) | Returns `true` when *name* is a Deprecated client capability. Mirrors TS `isDeprecatedClientCapability`. |
| static [IsDeprecatedServerCapability](CapabilityNegotiation/IsDeprecatedServerCapability.md)(…) | Returns `true` when *name* is a Deprecated server capability. Mirrors TS `isDeprecatedServerCapability`. |
| static [MayClientInvoke](CapabilityNegotiation/MayClientInvoke.md)(…) | Returns `true` when a client MAY invoke *method* given the server's declared capabilities (R-6.3-e, R-6.4-f, R-6.4-g). An ungated core method is always invocable; a gated method requires its governing capability to be declared. Mirrors TS `mayClientInvoke`. |
| static [MayInvokeRootsList](CapabilityNegotiation/MayInvokeRootsList.md)(…) | A server MUST NOT invoke `roots/list` unless `roots` is present (R-6.2-i). Mirrors TS `mayInvokeRootsList`. |
| static [MayInvokeSampling](CapabilityNegotiation/MayInvokeSampling.md)(…) | A server MUST NOT invoke `sampling/createMessage` unless `sampling` is present (R-6.2-l). Mirrors TS `mayInvokeSampling`. |
| static [MayUseIncludeContext](CapabilityNegotiation/MayUseIncludeContext.md)(…) | Returns whether a server MAY use a given `includeContext` value during sampling, given the client's capabilities (R-6.2-o). When `sampling.context` is absent the server SHOULD use only `"none"` (or omit the field); when present, any value is allowed. Mirrors TS `mayUseIncludeContext`. |
| static [MayUseSamplingTools](CapabilityNegotiation/MayUseSamplingTools.md)(…) | A server MUST NOT supply sampling `tools`/`toolChoice` unless `sampling.tools` is present (R-6.2-q). Mirrors TS `mayUseSamplingTools`. |
| static [MayUseUrlElicitation](CapabilityNegotiation/MayUseUrlElicitation.md)(…) | A server MUST NOT use URL-mode elicitation unless `elicitation.url` is present (R-6.2-g). Mirrors TS `mayUseUrlElicitation`. |
| static [NotificationRequiredCapability](CapabilityNegotiation/NotificationRequiredCapability.md)(…) | Returns the capability/sub-flag that gates *notification*, or `null` for an ungated one. Mirrors TS `notificationRequiredCapability`. |
| static [ServerDeclares](CapabilityNegotiation/ServerDeclares.md)(…) | Returns `true` when the server's raw capability map declares *capability* (§6.2). Object capabilities are declared by presence; the boolean sub-flags (`listChanged`, `subscribe`) are declared only when explicitly `true` — absent or `false` means not declared (R-6.3-h, R-6.3-l, R-6.3-o). Mirrors TS `serverDeclares`. |
| static [ServerMethodRequiredCapability](CapabilityNegotiation/ServerMethodRequiredCapability.md)(…) | Returns the capability that gates *method*, or `null` for an ungated (core) method. Mirrors TS `serverMethodRequiredCapability`. |

## Remarks

The predicates here read raw JsonObject capability maps (mirroring the TS `Record<string,unknown>` inputs) so they apply uniformly to capabilities arriving on the wire, before they are projected onto the typed [`ClientCapabilities`](./ClientCapabilities.md) / [`ServerCapabilities`](./ServerCapabilities.md) records. The typed records also expose equivalent `Supports*` / `Declares*` accessors for callers that already hold a parsed record.

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
