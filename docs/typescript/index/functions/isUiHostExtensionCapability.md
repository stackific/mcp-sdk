[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isUiHostExtensionCapability

# Function: isUiHostExtensionCapability()

> **isUiHostExtensionCapability**(`value`): `value is objectOutputType<{ mimeTypes: ZodArray<ZodString, "many"> }, ZodTypeAny, "passthrough">`

Defined in: [protocol/ui.ts:221](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L221)

Returns `true` when `value` is a well-formed [UiHostExtensionCapability](../type-aliases/UiHostExtensionCapability.md)
(a `mimeTypes` string array is present). This does NOT require the UI MIME
type to be present — use [capabilityRendersUi](capabilityRendersUi.md) for that. (R-26.2-d)

## Parameters

### value

`unknown`

## Returns

`value is objectOutputType<{ mimeTypes: ZodArray<ZodString, "many"> }, ZodTypeAny, "passthrough">`
