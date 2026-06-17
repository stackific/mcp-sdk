[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isCitationLoadBearing

# Function: isCitationLoadBearing()

> **isCitationLoadBearing**(`_citationMarker`): `boolean`

Defined in: [protocol/conformance-requirements.ts:1027](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/conformance-requirements.ts#L1027)

Returns `false` always: no §30 citation marker is ever load-bearing. (R-30-a)
Provided as a predicate so a conformance harness can assert that removing a
citation changes no required behavior — the answer is unconditionally "not
load-bearing", independent of which marker is named.

## Parameters

### \_citationMarker

`string`

## Returns

`boolean`
