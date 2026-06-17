[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / assertSafeInteger

# Function: assertSafeInteger()

> **assertSafeInteger**(`n`): `void`

Defined in: [json/value.ts:64](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/json/value.ts#L64)

Asserts that `n` is within the safe-integer range.
Senders MUST NOT emit identifier/counter values outside this range. (R-2.5-d)

## Parameters

### n

`number`

## Returns

`void`
