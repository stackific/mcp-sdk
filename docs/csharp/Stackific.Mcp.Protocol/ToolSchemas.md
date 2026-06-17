# ToolSchemas class

The §16.4 JSON-Schema hardening gate and the §16.7 tool-annotation resolution for the Tools feature, ported from the TypeScript SDK's `tools.ts` / `tools-call.ts`. These are the normative pure helpers that protect schema registration against resource exhaustion and SSRF and that apply the spec defaults to untrusted annotation hints.

```csharp
public static class ToolSchemas
```

## Public Members

| name | description |
| --- | --- |
| static [SupportedSchemaDialects](ToolSchemas/SupportedSchemaDialects.md) { get; } | The complete set of JSON Schema dialects this implementation can validate against (§16.4(9), R-16.4-s, R-16.4-u): ONLY JSON Schema 2020-12 — both the canonical `https://json-schema.org/draft/2020-12/schema` and its `#`-suffixed spelling. No dialect beyond 2020-12 is supported; a tool schema declaring any other `$schema` (for example Draft-07) is rejected by [`IsSupportedSchemaDialect`](./ToolSchemas/IsSupportedSchemaDialect.md) / [`ValidateToolSchema`](./ToolSchemas/ValidateToolSchema.md) rather than silently treated as permissive (R-16.4-t). |
| const [DefaultSchemaDialect](ToolSchemas/DefaultSchemaDialect.md) | The default JSON Schema dialect assumed when no explicit `$schema` is present (§16.4(1), R-16.4-a). |
| static [AssertRegistrableToolSchema](ToolSchemas/AssertRegistrableToolSchema.md)(…) | Asserts a tool schema is safe to register, throwing when it is not. A server MUST reject — or refuse to register — any schema it cannot safely validate. Throws [`UnsupportedDialectException`](./UnsupportedDialectException.md) for an unsupported dialect and ArgumentException for every other rejection. (§16.4(7)(9), R-16.4-n, R-16.4-t) |
| static [ConfigureValueValidator](ToolSchemas/ConfigureValueValidator.md)(…) | Installs the JSON Schema 2020-12 value validator used by [`ValidateValueAgainstSchema`](./ToolSchemas/ValidateValueAgainstSchema.md). Called once by the server's schema-validation module so the protocol layer can validate without a compile-time dependency on the engine. (R-16.4-o, R-16.4-p) |
| static [HasExternalRef](ToolSchemas/HasExternalRef.md)(…) | Walks a schema document and returns `true` when any `$ref` / `$dynamicRef` targets a location OUTSIDE the document. Such a reference MUST NOT be dereferenced or fetched over network or file system; this pure inspection never performs I/O (so it cannot itself trigger an SSRF fetch) — it only reports the presence of an external reference so callers can reject it. (§16.4(5), R-16.4-f, R-16.4-g, R-16.4-k, R-16.4-r) |
| static [IsInDocumentRef](ToolSchemas/IsInDocumentRef.md)(…) | Returns `true` when a `$ref` / `$dynamicRef` value resolves WITHIN the same schema document — a document-local JSON Pointer (`#`, `#/…`) or a plain-name fragment anchor (`#anchor`). An absolute or relative URI naming another document is NOT in-document. (§16.4(5), R-16.4-f) |
| static [IsSupportedSchemaDialect](ToolSchemas/IsSupportedSchemaDialect.md)(…) | Returns `true` when *dialect* is one this implementation supports (R-16.4-s, R-16.4-t). |
| static [SchemaDialect](ToolSchemas/SchemaDialect.md)(…) | Returns the dialect governing a schema document: the explicit `$schema` keyword when present (and a string), otherwise the default 2020-12 dialect. (§16.4(1), R-16.4-a, R-16.4-b) |
| static [SchemaNestingDepth](ToolSchemas/SchemaNestingDepth.md)(…) | Returns the maximum nesting depth of a schema document (objects + arrays). Counting stops at *cap* so a pathologically deep value cannot exhaust the stack. A leaf object such as `{"type":"object"}` has depth 1. (§16.4(6), R-16.4-l) |
| static [ValidateToolArguments](ToolSchemas/ValidateToolArguments.md)(…) | Validates a `tools/call``arguments` object against a tool's `inputSchema`. A receiver MUST validate arguments against the input schema. (R-16.4-o) |
| static [ValidateToolSchema](ToolSchemas/ValidateToolSchema.md)(…) | Validates a tool's `inputSchema` or `outputSchema` against the §16.4 hardening rules, WITHOUT any network or file-system retrieval. Returns a structured result rather than throwing so a caller can reject-or-refuse-registration. (§16.4, R-16.4-d/e/f/g/k/l/n/s/t) |
| static [ValidateToolStructuredContent](ToolSchemas/ValidateToolStructuredContent.md)(…) | Validates a tool result's `structuredContent` against the tool's `outputSchema`. When the tool declares no output schema there is nothing to validate and the result is valid; otherwise the value MUST conform. (R-16.4-p, R-16.5-o) |
| static [ValidateValueAgainstSchema](ToolSchemas/ValidateValueAgainstSchema.md)(…) | Validates a JSON value against a JSON Schema document (the 2020-12 dialect). This is the value-validation capability §16.4 places in this module: the machinery used to validate a `tools/call``arguments` object against an `inputSchema`, and a `structuredContent` value against an `outputSchema`. Never throws; returns `valid: false` when the schema is not a supported 2020-12 object schema or cannot be compiled. (§16.4, R-16.4-o, R-16.4-p) |

## Remarks

The actual value validation (an `arguments` object against an `inputSchema`, or a `structuredContent` value against an `outputSchema`) is delegated to `Stackific.Mcp.Server.SchemaValidation` via the ValueValidator hook, because it requires a full JSON Schema 2020-12 engine. The hardening checks here are pure and perform no I/O, so inspecting a schema for an external `$ref` never itself triggers a network/file fetch — it only reports its presence so the caller can reject it.

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
