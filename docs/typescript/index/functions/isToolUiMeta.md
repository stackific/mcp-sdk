[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isToolUiMeta

# Function: isToolUiMeta()

> **isToolUiMeta**(`value`): `value is objectOutputType<{ resourceUri: ZodEffects<ZodString, string, string>; visibility: ZodOptional<ZodArray<ZodEnum<["model", "app"]>, "many">> }, ZodTypeAny, "passthrough">`

Defined in: [protocol/ui.ts:467](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L467)

Returns `true` when `value` is a well-formed [ToolUiMeta](../type-aliases/ToolUiMeta.md). (§26.3)

## Parameters

### value

`unknown`

## Returns

`value is objectOutputType<{ resourceUri: ZodEffects<ZodString, string, string>; visibility: ZodOptional<ZodArray<ZodEnum<["model", "app"]>, "many">> }, ZodTypeAny, "passthrough">`
