# MetaKeys class

The reserved `_meta` keys defined by the protocol, plus the structural naming rules for any `_meta` key (spec §2.6.2 / §4.2, Appendix C).

```csharp
public static class MetaKeys
```

## Public Members

| name | description |
| --- | --- |
| const [Baggage](MetaKeys/Baggage.md) | Reserved bare key: W3C Baggage (§4.2, §15.4). |
| const [CanonicalPrefix](MetaKeys/CanonicalPrefix.md) | The canonical reverse-DNS prefix for keys defined by the MCP spec. |
| const [ClientCapabilities](MetaKeys/ClientCapabilities.md) | Reserved key carrying the per-request `ClientCapabilities` (§4.3). |
| const [ClientInfo](MetaKeys/ClientInfo.md) | Reserved key carrying the client `Implementation` on every client request (§4.3). |
| const [ProgressToken](MetaKeys/ProgressToken.md) | Reserved bare key: out-of-band progress-correlation token (§15.1). |
| const [ProtocolVersion](MetaKeys/ProtocolVersion.md) | Reserved key carrying the protocol revision on every client request (§4.3). |
| const [SubscriptionId](MetaKeys/SubscriptionId.md) | Reserved key correlating a notification with its subscription stream (§10). |
| const [TasksExtension](MetaKeys/TasksExtension.md) | Extension identifier for the Tasks extension (§25). |
| static readonly [TraceContextKeys](MetaKeys/TraceContextKeys.md) | The bare keys reserved for W3C trace-context propagation: `traceparent`, `tracestate`, and `baggage` (§2.6.2 / §4.2, R-2.6.2-i). These are always valid despite carrying no prefix — they are permitted by exception to the prefix rule. |
| const [TraceParent](MetaKeys/TraceParent.md) | Reserved bare key: W3C Trace Context `traceparent` (§4.2, §15.4). |
| const [TraceState](MetaKeys/TraceState.md) | Reserved bare key: W3C Trace Context `tracestate` (§4.2, §15.4). |
| const [UiExtension](MetaKeys/UiExtension.md) | Extension identifier for the Interactive User-Interface extension (§26). |
| static [IsReservedMetaKeyPrefix](MetaKeys/IsReservedMetaKeyPrefix.md)(…) | Returns `true` if *prefix* is reserved — its second label is `modelcontextprotocol` or `mcp` (§2.6.2 / §4.2, R-2.6.2-f). Implementations MUST NOT define `_meta` keys under a reserved prefix except as specified by the protocol or an MCP-published extension. |
| static [IsReservedPrefix](MetaKeys/IsReservedPrefix.md)(…) | Returns `true` if *key* sits under a protocol-reserved prefix — its second label is `modelcontextprotocol` or `mcp` (§4.2). Third parties MUST NOT mint keys under such a prefix. |
| static [IsValidBaggage](MetaKeys/IsValidBaggage.md)(…) | Returns `true` when *value* conforms to the W3C Baggage grammar: each comma-separated list member is `token "=" *baggage-octet` with optional semicolon-separated properties. (R-4.2-m, AC-05.15) |
| static [IsValidKey](MetaKeys/IsValidKey.md)(…) | Returns `true` if *key* is a syntactically valid `_meta` key AND its prefix (if present) is not reserved (§2.6.2 / §4.2, R-2.6.2-i, R-2.6.2-j). |
| static [IsValidMetaKeyName](MetaKeys/IsValidMetaKeyName.md)(…) | Returns `true` if *name* is a valid `_meta` key name. An empty name is valid (it occurs when a prefix is present and nothing follows the slash). A non-empty name MUST begin and end with an alphanumeric; interior characters MAY be alphanumerics, hyphens, underscores, or dots. (R-2.6.2-g, R-2.6.2-h, AC-02.18) |
| static [IsValidMetaKeyPrefix](MetaKeys/IsValidMetaKeyPrefix.md)(…) | Returns `true` if *prefix* is a syntactically valid `_meta` key prefix: one or more dot-separated labels terminated by a single `/`. (R-2.6.2-b, R-2.6.2-c, R-2.6.2-d, AC-02.17) |
| static [IsValidTraceContextValue](MetaKeys/IsValidTraceContextValue.md)(…) | Returns `true` when *value* is a valid W3C `tracestate` OR `baggage` value — accepted if either grammar matches. (R-2.6.2-i) |
| static [IsValidTraceparent](MetaKeys/IsValidTraceparent.md)(…) | Returns `true` when *value* conforms to the W3C `traceparent` format `{version}-{traceId}-{parentId}-{flags}` (`00-32hex-16hex-2hex`), with lowercase hex only. (R-2.6.2-i, AC-02.19) |
| static [IsValidTracestate](MetaKeys/IsValidTracestate.md)(…) | Returns `true` when *value* conforms to the W3C Trace Context `tracestate` grammar: each list member is a `simple-key=value` or `tenant-id@system-id=value` pair, with up to 32 comma-separated members and a total length of at most 512 characters. (R-4.2-l, AC-05.15) |
| static [ParseMetaKey](MetaKeys/ParseMetaKey.md)(…) | Splits a `_meta` key into its prefix (if any) and name using the first slash as the separator. The prefix includes the trailing slash; the name is everything after it. A key with no slash has a `null` prefix and the whole key as its name. |
| struct [ParsedMetaKey](MetaKeys.ParsedMetaKey.md) | The prefix (with trailing slash) and name parts of a parsed `_meta` key. |

## Remarks

A valid `_meta` key is an OPTIONAL prefix followed by a name. The prefix, when present, is one or more dot-separated labels terminated by a single slash; each label starts with a letter and ends with a letter or digit, with letters, digits, or hyphens in between, and SHOULD use reverse-DNS notation (for example `com.example/`). A prefix whose second label is `modelcontextprotocol` or `mcp` is reserved for the protocol. The name (after the prefix, or the whole key when there is no prefix) is either empty or begins and ends with an alphanumeric, with alphanumerics, hyphens, underscores, or dots in between.

The SDK never rejects an inbound message merely for carrying an unknown `_meta` key — §4.1 requires unknown keys to be ignored — so the validation helpers here exist for constructing well-formed metadata and for diagnostics, not for gating receipt.

## See Also

* namespace [Stackific.Mcp.Json](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
