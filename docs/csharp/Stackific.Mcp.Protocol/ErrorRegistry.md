# ErrorRegistry class

The single, authoritative model for §22 error handling: the full registry of error codes, the classification taxonomy and code ranges, the reserved-code set and extension-code validator, the HTTP-status overlay, the inbound-failure-stage mapping, the canonical resource-not-found mapping, and the firm boundary between a protocol-level JSON-RPC error and a feature-level error result (a tool that ran and failed).

```csharp
public static class ErrorRegistry
```

## Public Members

| name | description |
| --- | --- |
| static [JsonRpcReservedRange](ErrorRegistry/JsonRpcReservedRange.md) { get; } | The JSON-RPC 2.0 reserved range for pre-defined errors: `-32768..-32000` inclusive. Codes outside this range are available for application use (§22.2, §22.7). |
| static [Registry](ErrorRegistry/Registry.md) { get; } | The complete §22 error-code registry, keyed by numeric code. The same `code` applies on every transport; the optional [`HttpStatus`](./ErrorCodeRegistryEntry/HttpStatus.md) is the Streamable HTTP overlay (§22.6). Note that `-32602` has a single entry even though several distinct conditions collapse onto it — the code is the key; the condition is conveyed by message/data. |
| static [ReservedErrorCodes](ErrorRegistry/ReservedErrorCodes.md) { get; } | The reserved codes an extension-defined code MUST NOT collide with: the five standard JSON-RPC codes, the two protocol-specific codes, and the `-32001` HeaderMismatch transport code (R-22.7-c, AC-34.23). |
| static [ServerErrorRange](ErrorRegistry/ServerErrorRange.md) { get; } | The implementation-defined server-error sub-range `-32099..-32000` inclusive, nested inside the reserved range; `-32001` HeaderMismatch lives here (§22.7). |
| const [InvalidCursorCode](ErrorRegistry/InvalidCursorCode.md) | The `-32602` alias used when an invalid or expired cursor is supplied (§18, §22.4). It is the SAME value as [`InvalidParams`](../Stackific.Mcp.JsonRpc/ErrorCodes/InvalidParams.md) — a documented alias, never a distinct code. |
| const [ResourceNotFoundLegacyCode](ErrorRegistry/ResourceNotFoundLegacyCode.md) | The legacy MCP "Resource not found" error code literal, `-32002` (§22.4). |
| static [BuildErrorObject](ErrorRegistry/BuildErrorObject.md)(…) | Builds a canonical error object. When *message* is omitted, the registry's condition name is used so the result always has a non-empty message (falling back to `"Error"` for an unregistered code). (R-22.1-c, R-22.1-i, R-22.1-k) |
| static [BuildNullIdParseErrorResponse](ErrorRegistry/BuildNullIdParseErrorResponse.md)(…) | Builds the `null`-id parse-error response for unparseable input — the one circumstance in which an error response's id need not match a request id (R-22.1-f, R-22.6-h, AC-34.4). |
| static [BuildResourceNotFoundParamsError](ErrorRegistry/BuildResourceNotFoundParamsError.md)(…) | Builds a `-32602` Invalid params resource-not-found error whose `data` includes the requested *uri*, per the §22.4 canonical mapping (R-22.4-g/h). A non-existent resource MUST be signalled this way and MUST NOT be signalled by an empty `contents` array (R-22.4-i). |
| static [ClassifyErrorCode](ErrorRegistry/ClassifyErrorCode.md)(…) | Classifies any integer *code* into one of the [`ErrorCodeClass`](./ErrorCodeClass.md) ranges, even codes not present in the registry. A registry entry's own class always wins; otherwise the code is placed by range (R-22.7-a). |
| static [ClassifyToolCallFailure](ErrorRegistry/ClassifyToolCallFailure.md)(…) | Decides whether a `tools/call` failure is reported as a JSON-RPC protocol error (`-32602`) or as a successful result with `isError: true` (R-22.5-a..f, AC-34.18). |
| static [DescribeUnknownErrorCode](ErrorRegistry/DescribeUnknownErrorCode.md)(…) | Surfaces an error response carrying a code the receiver does not recognise. Per R-22.7-e a receiver MUST treat an unknown code as a failed request and surface it using the message and data, NOT reject it as malformed (AC-34.24). |
| static [ErrorCodeForInboundFailure](ErrorRegistry/ErrorCodeForInboundFailure.md)(…) | Selects the authoritative `error.code` for a failed-inbound-message stage, per the §22.6 transport mapping (R-22.6-b..f). |
| static [HttpStatusForRegistryCode](ErrorRegistry/HttpStatusForRegistryCode.md)(…) | Maps an error *code* to the Streamable HTTP status it MUST ride on (§22.6). `-32003`/`-32004` (negotiation) and `-32001` (HeaderMismatch) all map to `400`; codes the registry does not pin to a status return `null`. |
| static [IsErrorCodeInClass](ErrorRegistry/IsErrorCodeInClass.md)(…) | Validates that *code* is allowed for the given classification (§22.2, §22.7). |
| static [IsReservedErrorCode](ErrorRegistry/IsReservedErrorCode.md)(…) | Returns `true` when *code* is one of the eight reserved codes (R-22.7-c). |
| static [LookupErrorCode](ErrorRegistry/LookupErrorCode.md)(…) | Looks up the registry entry for *code*, or `null` if it is not a §22 registry code. An absent entry is not an error — receivers MUST tolerate unknown codes (see [`DescribeUnknownErrorCode`](./ErrorRegistry/DescribeUnknownErrorCode.md)). (R-22.7-e) |
| static [ValidateExtensionErrorCode](ErrorRegistry/ValidateExtensionErrorCode.md)(…) | Validates that *code* is a legal extension-defined error code: an integer that does not collide with any reserved code (R-22.7-a..c). (2 methods) |

## Remarks

The numeric codes themselves already live on [`ErrorCodes`](../Stackific.Mcp.JsonRpc/ErrorCodes.md) (the wire contract); this type re-uses those constants and never redeclares a value. The one code owned by a concurrent feature module — `-32002` Resource not found — is pinned here as [`ResourceNotFoundLegacyCode`](./ErrorRegistry/ResourceNotFoundLegacyCode.md) so the registry is complete without a forward dependency.

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
