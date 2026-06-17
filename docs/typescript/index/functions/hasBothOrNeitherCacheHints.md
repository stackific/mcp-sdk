[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / hasBothOrNeitherCacheHints

# Function: hasBothOrNeitherCacheHints()

> **hasBothOrNeitherCacheHints**(`result`): `boolean`

Defined in: [protocol/caching.ts:106](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/caching.ts#L106)

Returns `true` when a result object carries BOTH caching-hint fields (or
neither). A server MUST NOT emit exactly one without the other. (R-13.1-g)

Pass a raw result object; this is a conformance check on server output.

## Parameters

### result

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
