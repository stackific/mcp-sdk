[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / effectiveVisibility

# Function: effectiveVisibility()

> **effectiveVisibility**(`meta`): readonly (`"model"` \| `"app"`)[]

Defined in: [protocol/ui.ts:516](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L516)

Returns the EFFECTIVE visibility of a UI declaration: the declared
`visibility` array when present, otherwise the default `["model","app"]`.
(§26.3, R-26.3-d)

## Parameters

### meta

`Pick`\<[`ToolUiMeta`](../type-aliases/ToolUiMeta.md), `"visibility"`\>

A [ToolUiMeta](../type-aliases/ToolUiMeta.md) (or its `visibility` may be omitted).

## Returns

readonly (`"model"` \| `"app"`)[]
