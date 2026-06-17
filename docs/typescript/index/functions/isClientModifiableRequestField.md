[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isClientModifiableRequestField

# Function: isClientModifiableRequestField()

> **isClientModifiableRequestField**(`field`): field is "metadata" \| "systemPrompt" \| "includeContext" \| "temperature" \| "stopSequences"

Defined in: [protocol/sampling.ts:833](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L833)

Returns `true` when `field` is one the client MAY modify/omit. (R-21.2.10-e)

## Parameters

### field

`string`

## Returns

field is "metadata" \| "systemPrompt" \| "includeContext" \| "temperature" \| "stopSequences"
