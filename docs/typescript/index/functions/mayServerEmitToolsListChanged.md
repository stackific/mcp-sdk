[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayServerEmitToolsListChanged

# Function: mayServerEmitToolsListChanged()

> **mayServerEmitToolsListChanged**(`serverCaps`): `boolean`

Defined in: [protocol/tools.ts:138](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L138)

Returns `true` when the server MAY emit `notifications/tools/list_changed` —
only when it declared `tools.listChanged: true`. When the flag is absent or
`false` the server does not emit that notification. (§16.1, R-16.1-b;
delegates to S10 `serverDeclares(caps, 'tools.listChanged')`.)

## Parameters

### serverCaps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
