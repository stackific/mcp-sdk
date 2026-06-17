[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isRootsDeprecated

# Function: isRootsDeprecated()

> **isRootsDeprecated**(): `boolean`

Defined in: [protocol/roots.ts:97](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L97)

Returns `true` — the `roots` capability is Deprecated in this revision.
(R-21-a, R-21.1-a, R-21.1.1-a · SHOULD NOT; AC-32.1)

Thin, intention-revealing wrapper over
`isDeprecatedClientCapability('roots')` so callers can assert the deprecation
status without hard-coding the name.

## Returns

`boolean`
