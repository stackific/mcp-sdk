# MultiRoundTrip class

Pure, side-effect-free helpers for the multi-round-trip mechanism (spec §11): the recognized input-request kinds and their required client capabilities (§11.2), duplicate-`inputRequests`-key detection and strict parsing (§11.2, R-11.2-f), the opaque `requestState` continuation-token codec (§11.3 — attacker-controlled, validated on decode), input-response key/kind correlation (§11.4), the client-facing result-type discrimination (§11.5), and the load-shedding / loop-guard / backoff / re-request helpers (§11.5). The C# counterpart of the exported functions in the TypeScript `protocol/multi-round-trip.ts` (S17).

```csharp
public static class MultiRoundTrip
```

## Public Members

| name | description |
| --- | --- |
| static readonly [DeprecatedInputRequestMethods](MultiRoundTrip/DeprecatedInputRequestMethods.md) | The two input-request kinds that are Deprecated client-provided capabilities (§11.2, §27.3). |
| static readonly [ParticipatingMethods](MultiRoundTrip/ParticipatingMethods.md) | The three methods that MAY return `input_required` results (§11.6, R-11.6-a). |
| static readonly [RecognizedInputRequestMethods](MultiRoundTrip/RecognizedInputRequestMethods.md) | The three recognized `InputRequest.method` values (§11.2, R-11.2-k). |
| static [BuildMissingCapabilityError](MultiRoundTrip/BuildMissingCapabilityError.md)(…) | Builds the `-32003` error a server returns when it cannot complete without an input-request kind the client did not declare (§11.5, R-11.5-i, R-11.5-j). On HTTP the response status MUST be 400. The `data.requiredCapabilities` names the unsupported capabilities. |
| static [BuildReRequestInputRequiredResult](MultiRoundTrip/BuildReRequestInputRequiredResult.md)(…) | Builds a NEW [`InputRequiredResult`](./InputRequiredResult.md) re-requesting only the still-missing input, or `null` when the retry supplied everything (§11.5, R-11.5-q). A server whose retry `inputResponses` is well-formed but incomplete SHOULD re-request the missing information rather than failing the request. |
| static [ClientSupportsInputRequestKind](MultiRoundTrip/ClientSupportsInputRequestKind.md)(…) | Returns `true` when the client declared support for the capability an input-request *method* requires (§11.2, §11.5, R-11.2-j, R-11.5-a). Used both server-side (may a server emit a kind) and client-side (may a client fulfill it). An unrecognized method is never supported. |
| static [ComputeMissingInputResponseKeys](MultiRoundTrip/ComputeMissingInputResponseKeys.md)(…) | Returns the `inputRequests` keys that the retry's *inputResponses* did not answer (§11.5, R-11.5-q). |
| static [ComputeRetryBackoffMs](MultiRoundTrip/ComputeRetryBackoffMs.md)(…) | Computes an exponential-backoff delay (ms) for the *attempt*-th retry on repeated non-progress (§11.5, R-11.5-n). A client retrying without progress SHOULD apply a reasonable backoff. |
| static [DiscriminateResultType](MultiRoundTrip/DiscriminateResultType.md)(…) | Branches on the `resultType` of a received result per the normative client-side rules of §11.5 (R-11.5-c, R-11.5-d, R-11.5-e, R-11.5-f, R-11.5-k): |
| static [IsDeprecatedInputRequestKind](MultiRoundTrip/IsDeprecatedInputRequestKind.md)(…) | Returns `true` when *method* is a Deprecated input-request kind (§11.2, R-11.2-i). Servers SHOULD prefer non-deprecated alternatives (e.g. `elicitation/create`). |
| static [IsLoadSheddingResult](MultiRoundTrip/IsLoadSheddingResult.md)(…) | Returns `true` when *result* is a load-shedding signal: `resultType` is `input_required`, `inputRequests` is absent or empty, and `requestState` is present (§11.5, R-11.5-l). A client MUST NOT treat this as an error; it MAY retry echoing `requestState`, applying backoff on repeated non-progress. |
| static [IsMrtrParticipatingMethod](MultiRoundTrip/IsMrtrParticipatingMethod.md)(…) | Returns `true` when *method* is one of the three MRTR-participating methods (R-11.6-a). |
| static [IsRecognizedInputRequestMethod](MultiRoundTrip/IsRecognizedInputRequestMethod.md)(…) | Returns `true` when *method* is one of the three recognized input-request kinds (R-11.2-k). |
| static [JsonHasDuplicateKeys](MultiRoundTrip/JsonHasDuplicateKeys.md)(…) | Scans raw JSON text for a duplicate object member name. JsonDocumentOptions) silently collapses duplicate keys (last-wins), so duplicate detection MUST work on the raw token stream — this tracks the member names seen within each object scope and reports the first repeat (§11.2, R-11.2-f). |
| static [MayEmitInputRequestKind](MultiRoundTrip/MayEmitInputRequestKind.md)(…) | Server-side gate: returns `true` when the server MAY emit an input-request of *method* given the client's declared capabilities (§11.2, §11.5, R-11.2-j, R-11.5-g). A server MUST NOT emit a kind the client has not declared. |
| static [ParseInputRequiredResult](MultiRoundTrip/ParseInputRequiredResult.md)(…) | Parses an [`InputRequiredResult`](./InputRequiredResult.md) from its raw JSON text, treating a duplicate object member name as malformed — the §11.2 rule that a receiver encountering duplicate `inputRequests` keys MUST treat the result as malformed (R-11.2-f), which is stricter than the base §2.3.1 last-wins tolerance. Duplicate detection runs on the raw text because the JSON parser would already have collapsed repeats. Use this instead of a plain parse when the raw wire text is available and duplicate-key strictness is required (TV-17.10). |
| static [RequiredClientCapabilityForInputRequest](MultiRoundTrip/RequiredClientCapabilityForInputRequest.md)(…) | Returns the client-capability name an input-request *method* requires, or `null` for an unrecognized method (§11.2, R-11.2-j). |
| static [ValidateInputResponseKeys](MultiRoundTrip/ValidateInputResponseKeys.md)(…) | Validates that every key in *inputResponses* was present in *inputRequests* (§11.2, §11.4, R-11.2-h, R-11.4-c, R-11.4-d). Reports the offending keys when any response key is not a known request key. |
| static [ValidateInputResponseKinds](MultiRoundTrip/ValidateInputResponseKinds.md)(…) | Validates that each value in *inputResponses* conforms to the expected response shape for the `InputRequest` kind sent under the same key (§11.4, R-11.4-e, R-11.4-f): `elicitation/create` → an `action`; `roots/list` → a `roots` array; `sampling/createMessage` → `role`, `content`, and `model`. Keys with no matching request are skipped (caught by [`ValidateInputResponseKeys`](./MultiRoundTrip/ValidateInputResponseKeys.md)); unrecognized request kinds are skipped. A mismatch lets a server reject the retry with a JSON-RPC error (R-11.5-s). |
| static [ValidateRetryParams](MultiRoundTrip/ValidateRetryParams.md)(…) | Validates the server-side retry params and returns a `-32602` error payload when *inputResponses* are malformed at the protocol level (§11.5, R-11.5-s). A server MUST return this error (not another `InputRequiredResult`) for a kind-mismatched retry. |
| static class [RequestStateCodec](MultiRoundTrip.RequestStateCodec.md) | The opaque `requestState` continuation token (§11.3): the server-minted, base64url-encoded JSON payload a client echoes verbatim on retry. It is ATTACKER-CONTROLLED — the client never parses or modifies it (R-11.3-a, R-11.3-b, R-11.3-f), and the server MUST validate it on decode and MUST NOT trust its contents (R-11.3-h, R-11.3-i). This codec captures the accumulated round count so a stateless server can resume where it left off; a real server may additionally sign/encrypt the payload (R-11.3-g), which is out of scope for the reference codec. |

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
