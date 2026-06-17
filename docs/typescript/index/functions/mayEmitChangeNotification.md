[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayEmitChangeNotification

# Function: mayEmitChangeNotification()

> **mayEmitChangeNotification**(`method`, `acknowledged`, `updatedUri?`): `boolean`

Defined in: [protocol/streaming.ts:496](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L496)

Returns `true` when the server MAY emit the change notification `method` on a
subscription stream whose acknowledged filter is `acknowledged`. A kind is
emittable ONLY when its filter field is reflected in the acknowledged filter —
which already encodes "requested AND capability-declared". (§10.5, R-10.1-d,
R-10.2-c, R-10.5-l)

For `notifications/resources/updated`, pass `updatedUri` so the per-resource
filter is also checked (R-10.2-l, R-10.2-m).

## Parameters

### method

`string`

### acknowledged

`objectOutputType`

### updatedUri?

`string`

## Returns

`boolean`
