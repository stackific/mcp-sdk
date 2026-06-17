[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ToolUiMetaSchema

# Variable: ToolUiMetaSchema

> `const` **ToolUiMetaSchema**: `ZodObject`\<\{ `resourceUri`: `ZodEffects`\<`ZodString`, `string`, `string`\>; `visibility`: `ZodOptional`\<`ZodArray`\<`ZodEnum`\<\[`"model"`, `"app"`\]\>, `"many"`\>\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `resourceUri`: `ZodEffects`\<`ZodString`, `string`, `string`\>; `visibility`: `ZodOptional`\<`ZodArray`\<`ZodEnum`\<\[`"model"`, `"app"`\]\>, `"many"`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `resourceUri`: `ZodEffects`\<`ZodString`, `string`, `string`\>; `visibility`: `ZodOptional`\<`ZodArray`\<`ZodEnum`\<\[`"model"`, `"app"`\]\>, `"many"`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/ui.ts:453](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L453)

`ToolUiMeta` — the object at a tool's `_meta.ui` declaring its associated
interactive UI. (§26.3)

Fields:
  - `resourceUri` REQUIRED: the `ui://`-scheme URI of the UI resource to render
    for this tool; the host reads it via `resources/read` for this EXACT URI.
    The schema enforces the `ui://` scheme so a non-`ui://` value is rejected
    as a UI association. (R-26.3-a, R-26.3-b, R-26.3-c)
  - `visibility` OPTIONAL: an array drawn from `"model"`/`"app"`; omitted ⇒
    `["model","app"]`. (R-26.3-d)

`.passthrough()` preserves forward-compatible members a later extension
version may add.
