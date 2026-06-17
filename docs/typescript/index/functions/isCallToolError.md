[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isCallToolError

# Function: isCallToolError()

> **isCallToolError**(`result`): `boolean`

Defined in: [protocol/tools-call.ts:263](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L263)

Returns whether a `CallToolResult` ended in a tool execution error, applying
the absent-⇒-`false` rule: `isError` absent is interpreted as `false`
(success). (§16.5, R-16.5-q; §16.6, R-16.6-b)

## Parameters

### result

#### isError?

`boolean`

## Returns

`boolean`
