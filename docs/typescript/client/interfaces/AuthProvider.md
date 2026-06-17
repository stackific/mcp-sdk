[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / AuthProvider

# Interface: AuthProvider

Defined in: [client/streamable-http.ts:51](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/streamable-http.ts#L51)

Supplies a bearer token for the protected-resource flow (§23.8).

## Methods

### token()

> **token**(): `string` \| `Promise`\<`string` \| `undefined`\> \| `undefined`

Defined in: [client/streamable-http.ts:57](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/streamable-http.ts#L57)

Returns the access token to attach as `Authorization: Bearer <token>`, or
`undefined`/empty to send the request unauthenticated. Resolved fresh on
every POST so a rotating token is always current.

#### Returns

`string` \| `Promise`\<`string` \| `undefined`\> \| `undefined`
