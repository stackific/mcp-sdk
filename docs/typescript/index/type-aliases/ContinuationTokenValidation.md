[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ContinuationTokenValidation

# Type Alias: ContinuationTokenValidation\<S\>

> **ContinuationTokenValidation**\<`S`\> = \{ `ok`: `true`; `state`: `S`; \} \| \{ `ok`: `false`; `reason`: `"integrity-failure"` \| `"expired"` \| `"replayed"` \| `"unknown"`; `detail`: `string`; \}

Defined in: [protocol/security.ts:953](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L953)

Outcome of validateContinuationToken.

## Type Parameters

### S

`S` = `unknown`
