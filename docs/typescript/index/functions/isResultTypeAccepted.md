[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isResultTypeAccepted

# Function: isResultTypeAccepted()

> **isResultTypeAccepted**(`resultType`, `activeSet`, `activeContributions?`): `boolean`

Defined in: [protocol/extension-mechanism.ts:389](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extension-mechanism.ts#L389)

Returns `true` when `resultType` is accepted: it is a core value, or it is
contributed by an extension that is in the active set. (R-24.5-e, R-24.5-f)

A value that is neither core nor contributed by an active extension is
INVALID — this returns `false`, and the receiver MUST treat the response as an
error (per §3.6 / S04 `interpretResultType`).

## Parameters

### resultType

`string`

### activeSet

`Iterable`\<`string`\>

### activeContributions?

`ReadonlyMap`\<`string`, `Iterable`\<`string`, `any`, `any`\>\> = `...`

## Returns

`boolean`
