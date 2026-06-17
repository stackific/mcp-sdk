[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayReadResource

# Function: mayReadResource()

> **mayReadResource**(`serverCaps`): `boolean`

Defined in: [protocol/resources-read.ts:227](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L227)

Returns `true` when `method` is `resources/read` AND the server declared the
`resources` capability — i.e. a server MAY accept the read and a client MAY
issue it. Reuses [mayAcceptResourceRequest](mayAcceptResourceRequest.md) (S26), which already gates
`resources/read` on the `resources` capability. (§17.1 via §17.5)

## Parameters

### serverCaps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
