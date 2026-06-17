[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isPkceSupportConfirmed

# Function: isPkceSupportConfirmed()

> **isPkceSupportConfirmed**(`metadata`): `boolean`

Defined in: [protocol/authorization-flow.ts:826](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L826)

Returns `true` when AS metadata confirms PKCE `S256` support. (R-28.5-k)

## Parameters

### metadata

`Pick`\<[`AuthorizationServerMetadata`](../type-aliases/AuthorizationServerMetadata.md), `"code_challenge_methods_supported"`\>

## Returns

`boolean`
