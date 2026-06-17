[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isReservedBareVendorPrefix

# Function: isReservedBareVendorPrefix()

> **isReservedBareVendorPrefix**(`prefix`): `boolean`

Defined in: [protocol/extension-mechanism.ts:107](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extension-mechanism.ts#L107)

Returns `true` when a vendor prefix is one of the bare reserved tokens
`modelcontextprotocol` or `mcp` (a single-label prefix with no dot). (R-24.2-f)

This is distinct from [isReservedExtensionPrefix](isReservedExtensionPrefix.md) (S11), which reserves a
prefix whose *second* label is reserved (e.g. `io.modelcontextprotocol`); a
bare single-label prefix has no second label, so that check alone would miss
`modelcontextprotocol/x` and `mcp/x`.

## Parameters

### prefix

`string`

## Returns

`boolean`
