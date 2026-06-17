[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isVisibleToModel

# Function: isVisibleToModel()

> **isVisibleToModel**(`meta`): `boolean`

Defined in: [protocol/ui.ts:555](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L555)

Returns `true` when a tool's effective visibility includes `"model"` — i.e. it
appears in the model's tool list and is callable via ordinary tool-calling. A
tool with `visibility` `["app"]` is callable ONLY by the UI and is HIDDEN from
the model's tool list, so this returns `false`. (§26.3, R-26.3-f)

## Parameters

### meta

`Pick`\<[`ToolUiMeta`](../type-aliases/ToolUiMeta.md), `"visibility"`\>

The tool's [ToolUiMeta](../type-aliases/ToolUiMeta.md).

## Returns

`boolean`
