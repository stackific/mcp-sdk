[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / emitDeprecationWarning

# Function: emitDeprecationWarning()

> **emitDeprecationWarning**(`feature`, `migration`): `void`

Defined in: [lifecycle/registry.ts:89](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/registry.ts#L89)

Emits a runtime deprecation warning through an environment-idiomatic
out-of-band mechanism (stderr or console.warn). (R-27.4-d, AC-43.26)

IMPORTANT: This function MUST NOT be called in a way that injects the
warning into the protocol wire format. It is advisory only and does not
alter message semantics. (R-27.4-e, AC-43.27)

## Parameters

### feature

`string`

The name of the deprecated feature being exercised.

### migration

`string`

The documented migration guidance.

## Returns

`void`
