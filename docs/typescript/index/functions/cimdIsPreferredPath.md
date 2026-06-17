[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / cimdIsPreferredPath

# Function: cimdIsPreferredPath()

> **cimdIsPreferredPath**(`clientSupportsCimd`, `serverSupportsCimd`): `boolean`

Defined in: [protocol/authorization-registration.ts:225](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-registration.ts#L225)

Returns `true` when both a client and an authorization server should prefer CIMD
as the registration path — both SHOULD support the mechanism. (R-23.12-a)

## Parameters

### clientSupportsCimd

`boolean`

Whether the client implements CIMD.

### serverSupportsCimd

`boolean`

Whether the AS advertises
  `client_id_metadata_document_supported: true`.

## Returns

`boolean`
