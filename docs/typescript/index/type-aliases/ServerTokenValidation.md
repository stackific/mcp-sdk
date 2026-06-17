[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ServerTokenValidation

# Type Alias: ServerTokenValidation

> **ServerTokenValidation** = \{ `ok`: `true`; \} \| \{ `ok`: `false`; `reason`: `string`; `code`: *typeof* [`RATE_LIMIT_REJECTION_CODE`](../variables/RATE_LIMIT_REJECTION_CODE.md); \}

Defined in: [protocol/security.ts:755](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L755)

Outcome of [validateServerAccessToken](../functions/validateServerAccessToken.md).
