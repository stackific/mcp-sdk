[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildListToolsRequest

# Function: buildListToolsRequest()

> **buildListToolsRequest**(`id`, `cursor?`, `extraMeta?`): `objectOutputType`

Defined in: [protocol/tools.ts:770](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L770)

Builds a `tools/list` JSON-RPC request. When `cursor` is supplied it is passed
through VERBATIM — the client MUST treat a received `nextCursor` as opaque and
MUST NOT parse or construct it. Omitting `cursor` requests the first page.
(§16.2, R-16.2-a, R-16.2-d, R-16.2-e, R-16.2-f)

## Parameters

### id

`string` \| `number`

The JSON-RPC request id.

### cursor?

`string`

OPTIONAL opaque cursor (e.g. a previously received `nextCursor`).

### extraMeta?

`Record`\<`string`, `unknown`\>

OPTIONAL additional `_meta` members.

## Returns

`objectOutputType`
