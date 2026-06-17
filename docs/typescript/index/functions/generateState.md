[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / generateState

# Function: generateState()

> **generateState**(`randomSource?`): `string`

Defined in: [protocol/authorization-flow.ts:645](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L645)

Generates an opaque, unguessable `state` value binding an authorization request
to the user-agent session. (R-23.5-g)

32 random bytes BASE64URL-encoded. Randomness is injectable for tests.

## Parameters

### randomSource?

(`size`) => `Buffer`

OPTIONAL byte source; defaults to `node:crypto`.

## Returns

`string`
