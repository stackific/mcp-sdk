# ElicitationForm class

S31 — Elicitation II: the restricted form-schema system, result-action semantics, the §20.6 completion notification handler, and the §20.7 consent/security helpers (spec §20.4–§20.8). The C# counterpart of the TypeScript `protocol/elicitation-form.ts` module.

```csharp
public static class ElicitationForm
```

## Public Members

| name | description |
| --- | --- |
| static [NumberSchemaTypes](ElicitationForm/NumberSchemaTypes.md) { get; } | The two permitted `NumberSchema.type` literals (spec §20.4, R-20.4-e). |
| static [SensitiveFieldMarkers](ElicitationForm/SensitiveFieldMarkers.md) { get; } | Heuristic markers for sensitive credential fields a server MUST NOT request via form mode (passwords, API keys, access tokens, payment credentials). Matched against a lower-cased field name / `title` / `description` (spec §20.7, R-20.7-h). This is a best-effort guard, not an exhaustive list; servers remain responsible for routing sensitive interactions to URL mode (R-20.7-i). |
| static [StringSchemaFormats](ElicitationForm/StringSchemaFormats.md) { get; } | The four permitted `StringSchema.format` literals (spec §20.4, R-20.4-d). A `format`, when present, MUST be exactly one of these; any other value (e.g. `"phone"`) is rejected. |
| const [ElicitationCompleteNotificationMethod](ElicitationForm/ElicitationCompleteNotificationMethod.md) | The exact method literal of the URL-mode out-of-band completion notification (spec §20.6, R-20.6-a). |
| static [ActionWireValue](ElicitationForm/ActionWireValue.md)(…) | The wire string of an [`ElicitationAction`](./ElicitationAction.md) enum value (spec §20.5, R-20.5-a). |
| static [AssertFormModeMayCollect](ElicitationForm/AssertFormModeMayCollect.md)(…) | Asserts that a form-mode `requestedSchema` does not request sensitive credential data — the §20.7 prohibition (spec §20.7, R-20.7-h, R-20.7-i). Returns `Ok` when no sensitive fields are detected, otherwise names the offending fields; the server MUST then use URL mode for those interactions instead (R-20.7-i). |
| static [BuildAcceptResult](ElicitationForm/BuildAcceptResult.md)(…) | Builds a form-mode `accept`[`ElicitResult`](./ElicitResult.md) carrying validated `content` (spec §20.5, R-20.5-c, R-20.5-i). Validates `content` against *requestedSchema* before building (the client-side pre-send check), so a malformed submission is rejected rather than sent. |
| static [BuildCancelResult](ElicitationForm/BuildCancelResult.md)() | Builds a `cancel`[`ElicitResult`](./ElicitResult.md) (no `content`) (spec §20.5). |
| static [BuildDeclineResult](ElicitationForm/BuildDeclineResult.md)() | Builds a `decline`[`ElicitResult`](./ElicitResult.md) (no `content`) (spec §20.5). |
| static [BuildElicitationCompleteNotification](ElicitationForm/BuildElicitationCompleteNotification.md)(…) | Builds a `notifications/elicitation/complete` notification for *elicitationId* as a raw JSON-RPC notification object (spec §20.6, R-20.6-a, R-20.6-b). The caller (the server) MUST send the result only to the client that initiated the elicitation (R-20.6-c) — a transport-level concern this builder cannot enforce; it ensures the `elicitationId` is carried verbatim. |
| static [BuildUrlAcceptResult](ElicitationForm/BuildUrlAcceptResult.md)() | Builds a URL-mode `accept`[`ElicitResult`](./ElicitResult.md) — consent to the out-of-band interaction, carrying NO `content` (spec §20.5, R-20.5-b). |
| static [BuildUrlConsentPresentation](ElicitationForm/BuildUrlConsentPresentation.md)(…) | Builds the consent-presentation data a client MUST show before opening a URL-mode elicitation URL: the full URL and a clearly-highlighted target host, plus warnings about Punycode / ambiguous URIs (spec §20.7, R-20.7-v, R-20.7-x). |
| static [CheckElicitationUrlSafety](ElicitationForm/CheckElicitationUrlSafety.md)(…) | Checks a server-constructed elicitation URL against the §20.7 safe-construction rules: it MUST NOT carry sensitive end-user info, MUST NOT be pre-authenticated to a protected resource, and SHOULD use HTTPS outside development (spec §20.7, R-20.7-p, R-20.7-q, R-20.7-s). |
| static [ClassifyEnumSchema](ElicitationForm/ClassifyEnumSchema.md)(…) | Classifies an enum schema into one of its five structural forms by the distinguishing keyword, or returns `null` when *value* is not a well-formed enum schema (spec §20.4). |
| static [ClassifyPrimitiveSchema](ElicitationForm/ClassifyPrimitiveSchema.md)(…) | Classifies a property schema by the `PrimitiveSchemaDefinition` member it selects, or returns `null` when it is not a valid primitive schema (spec §20.4). Selection is structural: boolean by `type`; number for `"number"`/`"integer"`; enum for a string/array schema carrying `enum`/`oneOf`/`items`; otherwise string for a plain `"string"`. |
| static [ExtractDefaults](ElicitationForm/ExtractDefaults.md)(…) | Extracts the per-field `default` values declared in a restricted form schema, so a defaults-supporting client can pre-populate the corresponding fields (spec §20.4, R-20.4-c). Returns a map from field name to its declared `default` node (cloned), including only the fields that declare one. A client that supports defaults SHOULD use these; one that does not MAY ignore them. |
| static [FindSensitiveFormFields](ElicitationForm/FindSensitiveFormFields.md)(…) | Inspects a form-mode `requestedSchema` for fields that appear to request sensitive credential data, which a server MUST NOT collect via form mode (and MUST instead route through URL mode) (spec §20.7, R-20.7-h, R-20.7-i). Returns the list of field names whose name / `title` / `description` matches a sensitive marker. An empty list means none were detected — general contact/profile data (name, email, username) is NOT categorically prohibited and is not flagged. |
| static [HandleElicitationComplete](ElicitationForm/HandleElicitationComplete.md)(…) | Decides how a client should react to an incoming elicitation-complete notification, enforcing the §20.6 ignore rule (spec §20.6, R-20.6-d, R-20.6-e). A client MUST ignore a notification whose `elicitationId` is unknown or already completed (R-20.6-d); for a still-pending id it MAY proceed to auto-retry, update its UI, or otherwise continue (R-20.6-e). Independently, a client SHOULD provide manual retry/cancel controls in case it never arrives (R-20.6-f) — a UI concern outside this pure decision. |
| static [IsBooleanSchema](ElicitationForm/IsBooleanSchema.md)(…) | Returns `true` when *value* is a valid `BooleanSchema` — a JSON object whose `type` is exactly `"boolean"` (spec §20.4, R-20.4-c). |
| static [IsElicitAction](ElicitationForm/IsElicitAction.md)(…) | Returns `true` when *value* is one of the three defined elicitation actions — `"accept"`, `"decline"`, or `"cancel"` (spec §20.5, R-20.5-a). |
| static [IsElicitationCompleteNotification](ElicitationForm/IsElicitationCompleteNotification.md)(…) | Returns `true` when *value* is a well-formed `notifications/elicitation/complete` JSON-RPC notification: `jsonrpc: "2.0"`, the exact method literal, and `params` carrying a non-empty `elicitationId` string (spec §20.6, R-20.6-a, R-20.6-b). |
| static [IsElicitContentValue](ElicitationForm/IsElicitContentValue.md)(…) | Returns `true` when *value* is a permitted `content` VALUE: a string, number, boolean, or array of strings — the only value types a form-mode `content` map may carry (spec §20.5, R-20.5-c). Objects, `null`, and mixed arrays are rejected. |
| static [IsEnumSchema](ElicitationForm/IsEnumSchema.md)(…) | Returns `true` when *value* is a well-formed enum schema in any of its five forms (spec §20.4). |
| static [IsLegacyTitledEnumSchema](ElicitationForm/IsLegacyTitledEnumSchema.md)(…) | Returns `true` when *value* is the Deprecated legacy enum form (a string `enum` carrying the non-standard `enumNames` parallel array). Useful for a conformance check that new functionality does not adopt it, while a legacy schema received from a peer is still accepted (spec §20.4, R-20.4-f). |
| static [IsNumberSchema](ElicitationForm/IsNumberSchema.md)(…) | Returns `true` when *value* is a valid `NumberSchema` — a JSON object whose `type` is `"number"` or `"integer"`, with any present `minimum`/`maximum` numeric (spec §20.4, R-20.4-c, R-20.4-e). |
| static [IsPrimitiveSchemaDefinition](ElicitationForm/IsPrimitiveSchemaDefinition.md)(…) | Returns `true` when *value* is a valid `PrimitiveSchemaDefinition` (spec §20.4). |
| static [IsRestrictedFormSchema](ElicitationForm/IsRestrictedFormSchema.md)(…) | Returns `true` when *value* is a valid restricted form `requestedSchema` (spec R-20.4-a). |
| static [IsStringSchema](ElicitationForm/IsStringSchema.md)(…) | Returns `true` when *value* is a valid free-text `StringSchema` — a JSON object whose `type` is exactly `"string"`, carrying no `enum`/`oneOf` (which would make it an enum member), with any present `minLength`/`maxLength` numeric and any present `format` one of the four permitted formats (spec §20.4, R-20.4-c, R-20.4-d). |
| static [IsStringSchemaFormat](ElicitationForm/IsStringSchemaFormat.md)(…) | Returns `true` when *value* is one of the four permitted string formats (spec R-20.4-d). |
| static [MayRenderUrlClickable](ElicitationForm/MayRenderUrlClickable.md)(…) | Returns `true` when a URL MAY be rendered as a clickable link for the given field, enforcing the §20.7 rule that ONLY the `url` field of a URL-mode request is clickable; no other field of any elicitation request may be (spec §20.7, R-20.7-r, R-20.7-y). |
| static [ResolveElicitActionOutcome](ElicitationForm/ResolveElicitActionOutcome.md)(…) | Maps a returned `ElicitResult` to the server's handling directive, encoding the §20.5 rule that a server MUST NOT assume success and MUST handle decline, cancel, and a client failure to process (spec §20.5, R-20.5-d … R-20.5-h). The returned [`Handle`](./ElicitationForm.ElicitActionOutcome/Handle.md) gives the server an explicit branch for every action — including `malformed` (the client's answer did not conform), which is treated as a failure to process, never as success. |
| static [ValidateElicitContent](ElicitationForm/ValidateElicitContent.md)(…) | Validates an accepted form-mode `content` map against the `requestedSchema` it answers, enforcing the §20.5 conformance rule: every value is a string, number, boolean, or array of strings; every value matches the type/constraints of its field; every `required` field is present; and no unknown field appears (spec §20.5, R-20.5-c). |
| static [ValidateElicitResult](ElicitationForm/ValidateElicitResult.md)(…) | Validates a returned `ElicitResult` against the §20.5 action/content rules for the mode it answers (spec §20.5, R-20.5-a, R-20.5-b, R-20.5-c). |
| static [ValidateRestrictedFormSchema](ElicitationForm/ValidateRestrictedFormSchema.md)(…) | Validates a form-mode `requestedSchema` against the FULL §20.4 restricted form schema: the outer object shape (`type: "object"`, a `properties` map, optional `required`/`$schema`) PLUS the requirement that every property is a valid `PrimitiveSchemaDefinition` (spec §20.4, R-20.4-a). |
| static [VerifyElicitationUserBinding](ElicitationForm/VerifyElicitationUserBinding.md)(…) | Verifies, for a URL-mode elicitation, that the user who opened the URL is the same user who started the elicitation — the §20.7 cross-user anti-phishing check (spec §20.7, R-20.7-j … R-20.7-o). |
| enum [ElicitActionHandling](ElicitationForm.ElicitActionHandling.md) | A structured directive for how a server should react to an `ElicitResult` (spec §20.5). |
| struct [ElicitActionOutcome](ElicitationForm.ElicitActionOutcome.md) | Outcome of [`ResolveElicitActionOutcome`](./ElicitationForm/ResolveElicitActionOutcome.md). |
| enum [ElicitationCompleteAction](ElicitationForm.ElicitationCompleteAction.md) | How a client should react to an incoming completion notification (spec §20.6). |
| struct [ElicitationCompleteHandling](ElicitationForm.ElicitationCompleteHandling.md) | Outcome of [`HandleElicitationComplete`](./ElicitationForm/HandleElicitationComplete.md). |
| enum [ElicitationCompleteIgnoreReason](ElicitationForm.ElicitationCompleteIgnoreReason.md) | The reason a completion notification is ignored (spec §20.6, R-20.6-d). |
| enum [ElicitationLifecycleState](ElicitationForm.ElicitationLifecycleState.md) | The state of an elicitation as tracked by a client awaiting URL-mode completion (spec §20.6). |
| struct [ElicitationUrlSafety](ElicitationForm.ElicitationUrlSafety.md) | Outcome of [`CheckElicitationUrlSafety`](./ElicitationForm/CheckElicitationUrlSafety.md). |
| struct [ElicitationUserBindingResult](ElicitationForm.ElicitationUserBindingResult.md) | Outcome of [`VerifyElicitationUserBinding`](./ElicitationForm/VerifyElicitationUserBinding.md). |
| struct [ElicitContentError](ElicitationForm.ElicitContentError.md) | One failure reported by [`ValidateElicitContent`](./ElicitationForm/ValidateElicitContent.md). |
| struct [ElicitContentValidation](ElicitationForm.ElicitContentValidation.md) | Outcome of [`ValidateElicitContent`](./ElicitationForm/ValidateElicitContent.md). |
| struct [ElicitResultError](ElicitationForm.ElicitResultError.md) | One failure reported by [`ValidateElicitResult`](./ElicitationForm/ValidateElicitResult.md). |
| struct [ElicitResultValidation](ElicitationForm.ElicitResultValidation.md) | Outcome of [`ValidateElicitResult`](./ElicitationForm/ValidateElicitResult.md). |
| enum [EnumSchemaForm](ElicitationForm.EnumSchemaForm.md) | The structural classification of an `EnumSchema`, by its distinguishing keyword (spec §20.4). |
| enum [PrimitiveSchemaKind](ElicitationForm.PrimitiveSchemaKind.md) | The structural classification of a `PrimitiveSchemaDefinition` (spec §20.4). |
| struct [RestrictedFormSchemaError](ElicitationForm.RestrictedFormSchemaError.md) | One failure reported by [`ValidateRestrictedFormSchema`](./ElicitationForm/ValidateRestrictedFormSchema.md). |
| struct [RestrictedFormSchemaValidation](ElicitationForm.RestrictedFormSchemaValidation.md) | Outcome of [`ValidateRestrictedFormSchema`](./ElicitationForm/ValidateRestrictedFormSchema.md). |
| struct [SensitiveFieldCheck](ElicitationForm.SensitiveFieldCheck.md) | Outcome of [`AssertFormModeMayCollect`](./ElicitationForm/AssertFormModeMayCollect.md). |
| struct [UnsafeUrlFinding](ElicitationForm.UnsafeUrlFinding.md) | A single unsafe-URL finding. |
| enum [UnsafeUrlReason](ElicitationForm.UnsafeUrlReason.md) | One reason an elicitation URL is unsafe, per the §20.7 server construction rules. |
| struct [UrlConsentPresentation](ElicitationForm.UrlConsentPresentation.md) | What a client must surface to the user before consenting to open a URL (spec §20.7). |
| enum [UserBindingFailure](ElicitationForm.UserBindingFailure.md) | The reason a URL-mode user-binding check fails (spec §20.7). |

## Remarks

S30 ([`Elicitation`](./Elicitation.md) / the wire records in `Elicitation.cs`) routed and modeled an elicitation: the capability, the input-required delivery of an `elicitation/create` request, and the `form`/`url` mode parameter shapes, including the structural `requestedSchema` container (a flat object of primitives). This module fills in the PAYLOAD and OUTCOME surface those modes require:

* the `PrimitiveSchemaDefinition` value type behind `requestedSchema.properties` — the four primitive field schemas (string / number / boolean) and the `EnumSchema` family (single/multi-select, titled or untitled, plus the Deprecated legacy `enumNames` form);
* a full §20.4 restricted-schema validator that checks each property is a valid primitive;
* a validator for the `content` a client returns on `accept` against that schema, and the `ElicitResult` action semantics (accept / decline / cancel, presence-of-content rules);
* the `notifications/elicitation/complete` server→client notification with its send/ignore rules (§20.6);
* the consent / security predicates: sensitive-data form-mode prohibition, URL-mode identity binding and anti-phishing checks, safe URL construction (server) and safe URL handling (client) (§20.7).

JSON Schema fragments are modeled as raw JsonNode / JsonObject values (mirroring the open, restricted-Schema shape carried in `requestedSchema.properties`); the classifiers and validators inspect them structurally, exactly as the TS module does. None of this code performs any I/O, rendering, or network access.

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
