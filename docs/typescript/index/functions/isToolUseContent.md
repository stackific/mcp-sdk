[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isToolUseContent

# Function: isToolUseContent()

> **isToolUseContent**(`block`): `block is objectOutputType<{ type: ZodLiteral<"tool_use">; id: ZodString; name: ZodString; input: ZodRecord<ZodString, ZodUnknown>; _meta: ZodOptional<ZodRecord<ZodString, ZodUnknown>> }, ZodTypeAny, "passthrough">`

Defined in: [protocol/sampling.ts:149](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L149)

Returns `true` when `block` is a `tool_use` content block.

## Parameters

### block

`unknown`

## Returns

`block is objectOutputType<{ type: ZodLiteral<"tool_use">; id: ZodString; name: ZodString; input: ZodRecord<ZodString, ZodUnknown>; _meta: ZodOptional<ZodRecord<ZodString, ZodUnknown>> }, ZodTypeAny, "passthrough">`
