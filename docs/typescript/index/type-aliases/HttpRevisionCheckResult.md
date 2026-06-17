[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / HttpRevisionCheckResult

# Type Alias: HttpRevisionCheckResult

> **HttpRevisionCheckResult** = \{ `ok`: `true`; \} \| \{ `ok`: `false`; `status`: *typeof* [`HTTP_REVISION_MISMATCH_STATUS`](../variables/HTTP_REVISION_MISMATCH_STATUS.md); `message`: `string`; \}

Defined in: [protocol/revision.ts:55](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/revision.ts#L55)

Outcome of `checkHttpRevisionHeader`.
