[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / getToolUiMeta

# Function: getToolUiMeta()

> **getToolUiMeta**(`tool`): `objectOutputType`\<\{ `resourceUri`: `ZodEffects`\<`ZodString`, `string`, `string`\>; `visibility`: `ZodOptional`\<`ZodArray`\<`ZodEnum`\<\[`"model"`, `"app"`\]\>, `"many"`\>\>; \}, `ZodTypeAny`, `"passthrough"`\> \| `undefined`

Defined in: [protocol/ui.ts:482](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L482)

Extracts the [ToolUiMeta](../type-aliases/ToolUiMeta.md) from a tool — i.e. parses `tool._meta.ui` —
returning `undefined` when there is no `_meta`, no `ui` key, or the value is
not a well-formed declaration. (§26.3)

This does NOT gate on negotiation: a receiver that has not negotiated the
extension MUST ignore the key (R-26.3-g) — use [readToolUiMeta](readToolUiMeta.md) for the
negotiation-aware read.

## Parameters

### tool

`unknown`

A tool object (or anything with an optional `_meta.ui`).

## Returns

`objectOutputType`\<\{ `resourceUri`: `ZodEffects`\<`ZodString`, `string`, `string`\>; `visibility`: `ZodOptional`\<`ZodArray`\<`ZodEnum`\<\[`"model"`, `"app"`\]\>, `"many"`\>\>; \}, `ZodTypeAny`, `"passthrough"`\> \| `undefined`
