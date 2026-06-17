# Registries class

S46 — Consolidated Registries: Methods, Errors, `_meta` Keys, Capabilities, and Types (spec Appendices A–E).

```csharp
public static class Registries
```

## Public Members

| name | description |
| --- | --- |
| static [AppendixBReservedCodeSet](Registries/AppendixBReservedCodeSet.md) { get; } | The reserved-code set surfaced as a convenience set (spec R-AppB-a, R-AppB-b). |
| static [CapabilityRegistry](Registries/CapabilityRegistry.md) { get; } | Appendix D — the Capability Registry: every client/server/extension capability defined by this document, with its side, its sub-flags (and their optionality, boolean-ness, and deprecation), and its defining section (spec Appendix D; R-AppD-a … f). |
| static [ErrorCodeRegistry](Registries/ErrorCodeRegistry.md) { get; } | Appendix B / §22 Error Code Registry as data: every code this document defines, mapped to a one-line description. A custom code MUST NOT equal any key here (R-AppB-a). |
| static [MetaKeyRegistry](Registries/MetaKeyRegistry.md) { get; } | Appendix C — the Reserved `_meta` Key Registry: every key reserved by this document that MAY appear in `_meta` (the `io.modelcontextprotocol/` prefixed keys plus the four bare-by-exception keys), each with where it is used, its meaning/requirement level, and its defining section (spec Appendix C; R-AppC-a … j). |
| static [MethodRegistry](Registries/MethodRegistry.md) { get; } | Appendix A — the Method and Notification Index: every JSON-RPC method and notification defined by the document and its extensions, with its kind, direction, and defining section (spec Appendix A). 28 entries. |
| static [ReservedErrorCodes](Registries/ReservedErrorCodes.md) { get; } | The eight codes a custom code MUST NOT collide with: the five standard JSON-RPC codes, the two protocol codes, and `-32001` HeaderMismatch (spec R-AppB-a). `-32001` is the member that lies inside the `-32000..-32099` server-error range. |
| static [ServerErrorRange](Registries/ServerErrorRange.md) { get; } | The reserved server-error range bounds: `{ Min = -32099, Max = -32000 }` (spec R-AppB-b). |
| static [TypeRegistry](Registries/TypeRegistry.md) { get; } | Appendix E — the Consolidated Type Index: every wire type (interface or type alias) declared by this document, in the published Appendix E order, each with its canonical defining section and a one-line purpose (spec Appendix E). 176 entries. |
| static [UiDialectMethodIndex](Registries/UiDialectMethodIndex.md) { get; } | The additional UI-dialect message names (§26) exchanged on the UI message channel, in scope ONLY when the user-interface extension is active — beyond the two handshake names already in [`MethodRegistry`](./Registries/MethodRegistry.md) (spec Appendix A). |
| const [ResourceNotFoundCode](Registries/ResourceNotFoundCode.md) | The legacy `-32002` resource-not-found code, listed in the registry for collision checks (spec §17.6 / Appendix B). |
| const [UiHostRequiredMimeType](Registries/UiHostRequiredMimeType.md) | The MIME type the `io.modelcontextprotocol/ui` host value's `mimeTypes` array MUST include (spec R-AppD-f, AC-46.18). |
| static [IsErrorCodeDefinedByDocument](Registries/IsErrorCodeDefinedByDocument.md)(…) | Returns `true` when *code* is a code the document already defines in Appendix B — a code a custom definition MUST avoid (spec R-AppB-a, AC-46.1). Consults the full [`ErrorCodeRegistry`](./Registries/ErrorCodeRegistry.md) so every listed code is caught, including the legacy resource-not-found literal and `-32001`. |
| static [IsMetaKeyPermitted](Registries/IsMetaKeyPermitted.md)(…) | Returns `true` when *key* MAY appear in `_meta` — either because it is a registry-reserved key (see [`IsReservedMetaKey`](./Registries/IsReservedMetaKey.md)) or because it is an extension-defined key carried under a valid non-reserved prefix (spec R-AppC-a, R-AppC-j, AC-46.3, AC-46.12). A bare key that is neither reserved-by-exception nor prefixed is NOT permitted. |
| static [IsRegisteredMethod](Registries/IsRegisteredMethod.md)(…) | Returns `true` when *name* appears in the core Appendix A index. |
| static [IsReservedMetaKey](Registries/IsReservedMetaKey.md)(…) | Returns `true` when *key* is reserved by this document and so MAY appear in `_meta` without being treated as an unknown/custom key: any key under the reserved `io.modelcontextprotocol/`/`mcp` prefix, or one of the four bare-by-exception keys (spec R-AppC-a, AC-46.3). Extension-defined keys outside the reserved prefix are NOT reserved by this predicate (use [`IsMetaKeyPermitted`](./Registries/IsMetaKeyPermitted.md) to confirm a key MAY appear at all). |
| static [LookupCapability](Registries/LookupCapability.md)(…) | Looks up the Appendix D entry for *capability*. When the same name is defined on more than one side (`extensions` is both a client and a server capability), pass *side* to disambiguate; otherwise the first match is returned (spec Appendix D). |
| static [LookupCapabilitySubFlag](Registries/LookupCapabilitySubFlag.md)(…) | Returns the named sub-flag of a capability, or `null` when the capability or the sub-flag is not defined (spec Appendix D). |
| static [LookupMetaKey](Registries/LookupMetaKey.md)(…) | Looks up the Appendix C entry for an exact reserved *key*, or `null` (spec Appendix C). Matches the literal rows only; use [`IsReservedMetaKey`](./Registries/IsReservedMetaKey.md) for the broader prefix-based reservation test. |
| static [LookupMethod](Registries/LookupMethod.md)(…) | Looks up the Appendix A entry for a method or notification *name*, searching the core index first and (when *includeUiDialect* is `true`) the UI-dialect names (spec Appendix A). Because a handful of UI-dialect names shadow core names, the core index is preferred unless a core hit is absent. |
| static [LookupType](Registries/LookupType.md)(…) | Looks up the Appendix E entry for a wire *type* name, or `null` (spec Appendix E). |
| static [MethodKindName](Registries/MethodKindName.md)(…) | The wire string for a [`RegistryMethodKind`](./Registries.RegistryMethodKind.md) (matching the TS table values). |
| static [RequiredClientRequestMetaKeys](Registries/RequiredClientRequestMetaKeys.md)() | Returns the reserved keys (Appendix C rows) that are REQUIRED on a client request (spec R-AppC-b … d). |
| static [ValidateCustomErrorCode](Registries/ValidateCustomErrorCode.md)(…) | Validates a custom error *code* against Appendix B's collision rule and flags whether a usable code lies inside the reserved `-32000..-32099` range (spec R-AppB-a, R-AppB-b, AC-46.1, AC-46.2). Delegates the integer/collision check to [`ValidateExtensionErrorCode`](./Registries/ValidateExtensionErrorCode.md) so the two stay in lockstep. |
| static [ValidateExtensionErrorCode](Registries/ValidateExtensionErrorCode.md)(…) | Validates an extension/custom error *code* against the §22 collision rule: it must be an integer and MUST NOT equal any reserved code (spec R-AppB-a). The double-typed parameter lets a caller pass a fractional value to exercise the integer check, mirroring the TS helper. |
| static [ValidateToolUiMetaValue](Registries/ValidateToolUiMetaValue.md)(…) | Validates a `Tool` object's `_meta.ui` value against Appendix C: it MUST be an object with a REQUIRED `resourceUri` that is a `ui://` URI and an OPTIONAL `visibility` (spec R-AppC-i, AC-46.11). Meaningful only when the UI extension is active. |
| static [ValidateUiHostValue](Registries/ValidateUiHostValue.md)(…) | Validates the `io.modelcontextprotocol/ui` host value against Appendix C/D: it MUST carry a `mimeTypes` array that includes [`UiHostRequiredMimeType`](./Registries/UiHostRequiredMimeType.md) (spec R-AppC-h, R-AppD-f, AC-46.10, AC-46.18). A server acknowledgement value MAY be empty — that case is the caller's to distinguish; this checks the host value. |
| record [CapabilityRegistryEntry](Registries.CapabilityRegistryEntry.md) | One row of Appendix D — a capability defined by this document (spec Appendix D). |
| record [CapabilitySubFlag](Registries.CapabilitySubFlag.md) | A single nested sub-flag of a capability, with its optionality and notes (spec Appendix D). |
| enum [CustomErrorCodeRejection](Registries.CustomErrorCodeRejection.md) | The reason a custom error code is rejected (spec R-AppB-a, R-AppB-b). |
| struct [CustomErrorCodeValidation](Registries.CustomErrorCodeValidation.md) | Outcome of [`ValidateExtensionErrorCode`](./Registries/ValidateExtensionErrorCode.md) / [`ValidateCustomErrorCode`](./Registries/ValidateCustomErrorCode.md). |
| record [MetaKeyRegistryEntry](Registries.MetaKeyRegistryEntry.md) | One row of Appendix C — a reserved key that MAY appear in `_meta`. |
| record [MethodNotificationIndexEntry](Registries.MethodNotificationIndexEntry.md) | One row of Appendix A — a single method or notification name. |
| enum [RegistryMethodKind](Registries.RegistryMethodKind.md) | Whether a name is a request, a notification, or an input-request kind delivered via §11 (spec Appendix A). |
| struct [ServerErrorRangeBounds](Registries.ServerErrorRangeBounds.md) | The bounds of the reserved server-error range `-32000..-32099` within which implementations MAY define additional codes, avoiding collision with the `-32001` HeaderMismatch code already placed there (spec R-AppB-b, AC-46.2). |
| enum [ToolUiMetaFailure](Registries.ToolUiMetaFailure.md) | The reason a tool `_meta.ui` value fails validation (spec R-AppC-i). |
| struct [ToolUiMetaValidation](Registries.ToolUiMetaValidation.md) | Outcome of [`ValidateToolUiMetaValue`](./Registries/ValidateToolUiMetaValue.md). |
| record [TypeIndexEntry](Registries.TypeIndexEntry.md) | One row of Appendix E — a named wire type declared by this document (spec Appendix E). |
| enum [UiHostValueFailure](Registries.UiHostValueFailure.md) | The reason a UI host value fails validation (spec R-AppC-h, R-AppD-f). |
| struct [UiHostValueValidation](Registries.UiHostValueValidation.md) | Outcome of [`ValidateUiHostValue`](./Registries/ValidateUiHostValue.md). |

## Remarks

The capstone reference artifact: five authoritative, document-wide tables that enumerate the wire surface defined across the whole specification, each row pointing to the section that normatively specifies the entry. These appendices define no new wire types — they are a consolidation, and the cited section remains normative.

The error-code registry (Appendix B) is reproduced here as queryable data ([`ErrorCodeRegistry`](./Registries/ErrorCodeRegistry.md), [`ReservedErrorCodes`](./Registries/ReservedErrorCodes.md), [`ServerErrorRange`](./Registries/ServerErrorRange.md)) together with the custom-code collision validators, because the C# SDK exposes the codes only as constants in [`ErrorCodes`](../Stackific.Mcp.JsonRpc/ErrorCodes.md) and has no registry-as-data equivalent.

## See Also

* namespace [Stackific.Mcp.Protocol](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
