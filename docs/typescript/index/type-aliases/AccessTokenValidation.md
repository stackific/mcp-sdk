[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / AccessTokenValidation

# Type Alias: AccessTokenValidation

> **AccessTokenValidation** = \{ `ok`: `true`; \} \| \{ `ok`: `false`; `challenge`: [`UnauthorizedChallenge`](../interfaces/UnauthorizedChallenge.md) \| [`InsufficientScopeChallenge`](../interfaces/InsufficientScopeChallenge.md); \}

Defined in: [protocol/authorization-flow.ts:1577](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L1577)

Outcome of [validateAccessTokenRequest](../functions/validateAccessTokenRequest.md).
