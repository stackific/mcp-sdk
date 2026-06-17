[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / clampToMaxTokens

# Function: clampToMaxTokens()

> **clampToMaxTokens**(`produced`, `maxTokens`): `number`

Defined in: [protocol/sampling.ts:486](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L486)

Clamps a produced token count to the request's `maxTokens` upper bound.
The client MAY sample fewer (R-21.2.4-i) but MUST NOT exceed `maxTokens`
(R-21.2.4-j). Returns the count unchanged when already within bound.

## Parameters

### produced

`number`

### maxTokens

`number`

## Returns

`number`
