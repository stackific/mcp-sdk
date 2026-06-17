[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isDeprecatedInputRequestKind

# Function: isDeprecatedInputRequestKind()

> **isDeprecatedInputRequestKind**(`method`): `boolean`

Defined in: [protocol/multi-round-trip.ts:754](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L754)

Returns `true` when `method` is a Deprecated input-request kind. Servers SHOULD
prefer non-deprecated alternatives (e.g. `elicitation/create`) where available
rather than soliciting via these. (§11.2 line 2406, R-11.2-i)

## Parameters

### method

`string`

## Returns

`boolean`
