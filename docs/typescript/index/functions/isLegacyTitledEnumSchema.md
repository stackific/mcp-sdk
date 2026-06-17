[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isLegacyTitledEnumSchema

# Function: isLegacyTitledEnumSchema()

> **isLegacyTitledEnumSchema**(`value`): `boolean`

Defined in: [protocol/elicitation-form.ts:391](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L391)

Returns `true` when `value` is the Deprecated [LegacyTitledEnumSchema](../variables/LegacyTitledEnumSchema.md)
form (a string `enum` carrying the non-standard `enumNames` parallel array).
Useful for a conformance check that new functionality does not adopt it, while
a legacy schema received from a peer is still accepted. (§20.4, R-20.4-f)

## Parameters

### value

`unknown`

## Returns

`boolean`
