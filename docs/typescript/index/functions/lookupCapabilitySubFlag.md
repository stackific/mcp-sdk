[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / lookupCapabilitySubFlag

# Function: lookupCapabilitySubFlag()

> **lookupCapabilitySubFlag**(`capability`, `subFlag`, `side?`): [`CapabilitySubFlag`](../interfaces/CapabilitySubFlag.md) \| `undefined`

Defined in: [protocol/registries.ts:569](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/registries.ts#L569)

Returns the named sub-flag of a capability, or `undefined` when the capability
or the sub-flag is not defined. Handy for asserting a sub-flag's optionality,
boolean-ness, or deprecation. (Appendix D)

## Parameters

### capability

`string`

### subFlag

`string`

### side?

[`CapabilitySide`](../type-aliases/CapabilitySide.md)

## Returns

[`CapabilitySubFlag`](../interfaces/CapabilitySubFlag.md) \| `undefined`
