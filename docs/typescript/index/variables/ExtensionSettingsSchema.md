[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ExtensionSettingsSchema

# Variable: ExtensionSettingsSchema

> `const` **ExtensionSettingsSchema**: `ZodRecord`\<`ZodString`, `ZodUnknown`\>

Defined in: [protocol/extensions.ts:162](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extensions.ts#L162)

Schema for a single extension settings object: any object, including the empty
object `{}`. (R-6.5-h) A `null` value is intentionally NOT accepted here
(R-6.5-i); receivers normalize a raw map with [normalizeExtensionsMap](../functions/normalizeExtensionsMap.md),
which drops `null`/malformed entries rather than rejecting the whole map.
