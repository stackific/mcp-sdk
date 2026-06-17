[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / UiToolResultOptions

# Interface: UiToolResultOptions

Defined in: [server/ui.ts:27](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/ui.ts#L27)

Options for [uiToolResult](../functions/uiToolResult.md).

## Properties

### text?

> `optional` **text?**: `string`

Defined in: [server/ui.ts:29](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/ui.ts#L29)

Leading text content block (a human-readable note).

***

### visibility?

> `optional` **visibility?**: (`"model"` \| `"app"`)[]

Defined in: [server/ui.ts:31](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/ui.ts#L31)

Which actors may invoke the tool; omitted ⇒ `["model","app"]`. (§26.3)
