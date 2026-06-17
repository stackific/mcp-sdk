[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isUiDialectMethodName

# Function: isUiDialectMethodName()

> **isUiDialectMethodName**(`name`): `name is UiDialectMethod`

Defined in: [protocol/ui-host.ts:238](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui-host.ts#L238)

Returns `true` when `name` is one of the verbatim dialect method/notification
names — matched byte-for-byte and case-sensitively, so `"UI/Initialize"` or
`"ui/Initialize"` do NOT match. (§26.6, R-26.5-a; AC-42.1)

## Parameters

### name

`unknown`

## Returns

`name is UiDialectMethod`
