[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / computeMissingInputResponseKeys

# Function: computeMissingInputResponseKeys()

> **computeMissingInputResponseKeys**(`inputRequests`, `inputResponses`): `string`[]

Defined in: [protocol/multi-round-trip.ts:805](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L805)

Returns the `inputRequests` keys that the retry's `inputResponses` did not
answer. (§11.5, R-11.5-q)

## Parameters

### inputRequests

`Record`\<`string`, `unknown`\>

### inputResponses

`Record`\<`string`, `unknown`\>

## Returns

`string`[]
