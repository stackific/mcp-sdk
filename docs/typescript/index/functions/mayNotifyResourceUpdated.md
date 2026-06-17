[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayNotifyResourceUpdated

# Function: mayNotifyResourceUpdated()

> **mayNotifyResourceUpdated**(`updatedUri`, `filter`): `boolean`

Defined in: [protocol/resources-read.ts:498](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L498)

Returns `true` when a server MAY send `notifications/resources/updated` for
`updatedUri` given the client's opted-in `resourceSubscriptions` filter — i.e.
the URI (or a parent container it is a sub-resource of) was listed. A server
MUST NOT send an update for any resource the client did not opt into. Reuses
S16's [mayDeliverResourceUpdate](mayDeliverResourceUpdate.md). (§17.7, R-17.7-i, R-17.7-j)

## Parameters

### updatedUri

`string`

The URI that changed.

### filter

`objectOutputType`

The §10 subscription filter the client opened the stream with.

## Returns

`boolean`
