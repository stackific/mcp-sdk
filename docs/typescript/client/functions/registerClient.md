[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / registerClient

# Function: registerClient()

> **registerClient**(`metadata`, `options`): `Promise`\<\{ `clientId`: `string`; `clientSecret?`: `string`; \}\>

Defined in: [client/oauth.ts:169](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/oauth.ts#L169)

Dynamic client registration (RFC 7591). (§23.4)

## Parameters

### metadata

`objectOutputType`

### options

#### clientName

`string`

#### redirectUris?

`string`[]

#### grantTypes?

`string`[]

#### applicationType?

`"native"` \| `"web"`

OAuth `application_type` — REQUIRED by §23.15; defaults to `'web'`.

#### fetch?

(`input`, `init?`) => `Promise`\<`Response`\>

## Returns

`Promise`\<\{ `clientId`: `string`; `clientSecret?`: `string`; \}\>
