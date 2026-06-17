[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CursorValidation

# Type Alias: CursorValidation

> **CursorValidation** = \{ `ok`: `true`; `cursor`: `string`; \} \| \{ `ok`: `false`; `error`: `ReturnType`\<*typeof* [`buildInvalidCursorError`](../functions/buildInvalidCursorError.md)\>; \}

Defined in: [protocol/security.ts:1434](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L1434)

Outcome of [validatePaginationCursor](../functions/validatePaginationCursor.md).
