[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayFetchDirectly

# Function: mayFetchDirectly()

> **mayFetchDirectly**(`uri`): `boolean`

Defined in: [protocol/resources-read.ts:602](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L602)

Returns `true` when a client MAY skip `resources/read` and fetch `uri`
directly from the web — true exactly when `uri` is an `https` resource URI.
(§17.5, R-17.5-y)

## Parameters

### uri

`string`

## Returns

`boolean`
