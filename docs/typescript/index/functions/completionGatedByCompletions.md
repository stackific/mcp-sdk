[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / completionGatedByCompletions

# Function: completionGatedByCompletions()

> **completionGatedByCompletions**(): `boolean`

Defined in: [protocol/completion.ts:169](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/completion.ts#L169)

Returns `true` when the S10 gate binds `completion/complete` to the
`completions` capability — a self-check that this module's method constant and
the shared capability map agree. (§6.3 / §19.1, R-19.1-a)

## Returns

`boolean`
