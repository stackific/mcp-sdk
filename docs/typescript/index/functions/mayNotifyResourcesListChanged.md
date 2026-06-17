[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayNotifyResourcesListChanged

# Function: mayNotifyResourcesListChanged()

> **mayNotifyResourcesListChanged**(`filter`): `boolean`

Defined in: [protocol/resources-read.ts:513](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L513)

Returns `true` when a server MAY deliver `notifications/resources/list_changed`
on a stream whose §10 filter is `filter` — only when the client opted in via
`resourcesListChanged: true`. A server MUST NOT deliver it on a stream that did
not request the filter. (§17.7, R-17.7-d, R-17.7-e)

## Parameters

### filter

`objectOutputType`

## Returns

`boolean`
