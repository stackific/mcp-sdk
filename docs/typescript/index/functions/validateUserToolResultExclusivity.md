[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / validateUserToolResultExclusivity

# Function: validateUserToolResultExclusivity()

> **validateUserToolResultExclusivity**(`message`): `object`

Defined in: [protocol/sampling.ts:660](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L660)

Validates the §21.2.7 content constraint on a single `user` message: when a
`user` message contains any `tool_result` block, it MUST contain ONLY
`tool_result` blocks — mixing with text/image/audio (or any other type) is
NOT allowed. (R-21.2.7-a)

Returns `{ ok: true }` for any non-`user` message, a `user` message with no
tool results, or a `user` message of only tool results. Returns
`{ ok: false, reason }` for a mixed `user` message.

## Parameters

### message

`objectOutputType`

## Returns

`object`

### ok

> **ok**: `boolean`

### reason?

> `optional` **reason?**: `string`
