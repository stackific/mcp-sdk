# Elicitation class

S30 — Elicitation I: the front-half capability/delivery/mode logic (spec §20.1–§20.3). The C# counterpart of the TypeScript `protocol/elicitation.ts` module: mode constants and resolution, server-side capability gating (R-20.1-d/e), URL validity, and the request builders.

```csharp
public static class Elicitation
```

## Public Members

| name | description |
| --- | --- |
| const [CreateMethod](Elicitation/CreateMethod.md) | The exact, case-sensitive `method` literal that identifies an elicitation input request within the multi-round-trip input-request union (spec §20.2, R-20.2-b). |
| const [FormMode](Elicitation/FormMode.md) | The form-mode discriminator value: in-band structured collection (spec §20.3, R-20.3-a). |
| const [UrlMode](Elicitation/UrlMode.md) | The url-mode discriminator value: out-of-band navigation (spec §20.3, R-20.3-i). |
| static [BuildFormElicitRequest](Elicitation/BuildFormElicitRequest.md)(…) | Builds a well-formed form-mode [`ElicitRequestFormParams`](./ElicitRequestFormParams.md) (spec §20.2, §20.3). The *requestedSchema* is validated against the §20.4 restricted form schema before the request is built (R-20.4); a malformed schema is rejected rather than sent. Form mode is the implicit baseline, so the `mode` field is not modeled on the record (it is emitted on the wire by the converter). |
| static [BuildUrlElicitRequest](Elicitation/BuildUrlElicitRequest.md)(…) | Builds a well-formed url-mode [`ElicitRequestURLParams`](./ElicitRequestURLParams.md) (spec §20.2, §20.3). The `url` is checked for validity (R-20.3-n) and the `elicitationId` for non-emptiness (R-20.3-k) before the request is built. |
| static [ClientSupportsElicitation](Elicitation/ClientSupportsElicitation.md)(…) | Returns `true` when *clientCaps* declares the `elicitation` capability — the MUST-declare-to-use rule (spec §20.1, R-20.1-a). A client that does not declare it is treated as not supporting elicitation. |
| static [ClientSupportsElicitationMode](Elicitation/ClientSupportsElicitationMode.md)(…) | Returns `true` when the client declaring *clientCaps* supports *mode*, applying the empty-object-equals-form-only equivalence (spec §20.1, R-20.1-c, R-20.1-f). `form` is supported whenever `elicitation` is declared; `url` requires the `elicitation.url` sub-flag. |
| static [GateElicitationRequest](Elicitation/GateElicitationRequest.md)(…) | Decides whether a server MAY send an `elicitation/create` request of *mode* to a client with the given declared capabilities (spec §20.1, R-20.1-d, R-20.1-e). A server MUST NOT return such a request to a client that has not declared `elicitation` (CapabilityNotDeclared), nor one whose `mode` the client's declared sub-flags do not support (ModeNotSupported). |
| static [IsElicitationCreateRequest](Elicitation/IsElicitationCreateRequest.md)(…) | Returns `true` when *value* carries the exact, case-sensitive `"elicitation/create"` method literal (spec §20.2, R-20.2-b). This is a lightweight method-only check; it does not validate `params`. |
| static [IsElicitationMode](Elicitation/IsElicitationMode.md)(…) | Returns `true` when *value* is one of the two defined elicitation modes (spec §20.3). |
| static [IsValidElicitationUrl](Elicitation/IsValidElicitationUrl.md)(…) | Returns `true` when *url* is a valid, absolute URI/URL — the requirement on the url-mode `url` field (spec §20.3, R-20.3-m, R-20.3-n). Relative references and malformed strings are rejected. |
| static [MayServerSendElicitation](Elicitation/MayServerSendElicitation.md)(…) | Convenience predicate: `true` exactly when [`GateElicitationRequest`](./Elicitation/GateElicitationRequest.md) permits a server to send an `elicitation/create` request of *mode* (spec §20.1, R-20.1-d, R-20.1-e). |
| static [ResolveElicitationMode](Elicitation/ResolveElicitationMode.md)(…) | Resolves the effective elicitation mode of a `params` object, applying the backwards-compatibility rule that an absent `mode` means form mode (spec §20.3, R-20.3-b, R-20.3-c). Returns `"form"` when `mode` is absent or the literal `"form"`, `"url"` when it is the literal `"url"`, and `null` for any other (malformed) value. |
| static [SupportedElicitationModes](Elicitation/SupportedElicitationModes.md)(…) | Returns the set of elicitation modes a client supports, applying the empty-object-equals-form-only equivalence: declaring `elicitation` always implies `form` (the implicit baseline), and `url` is added only when the `elicitation.url` sub-flag is present (spec §20.1, R-20.1-c, R-20.1-f). Returns an empty list when `elicitation` is not declared at all. |
| enum [ElicitationGateRejection](Elicitation.ElicitationGateRejection.md) | Why a server may not emit an `elicitation/create` request, per the §20.1 gating rules. |
| struct [ElicitationGateResult](Elicitation.ElicitationGateResult.md) | Outcome of [`GateElicitationRequest`](./Elicitation/GateElicitationRequest.md). |

## Remarks

An elicitation request is NOT a server-initiated JSON-RPC request: the server returns an `input_required` result carrying an `elicitation/create` request (the §11 multi-round-trip mechanism), and the client supplies the user's input by retrying the originating request. This class owns the rules around whether a server may send such a request and how to build it; the wire records ([`ElicitRequestParams`](./ElicitRequestParams.md) and friends) and the payload/outcome surface ([`ElicitationForm`](./ElicitationForm.md)) live elsewhere.

Capability gating reuses the foundation's raw-map predicates ([`ClientDeclares`](./CapabilityNegotiation/ClientDeclares.md) and [`MayUseUrlElicitation`](./CapabilityNegotiation/MayUseUrlElicitation.md)) so the rules apply uniformly to a client capabilities map arriving on the wire.

## See Also

* namespace [Stackific.Mcp.Protocol](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
