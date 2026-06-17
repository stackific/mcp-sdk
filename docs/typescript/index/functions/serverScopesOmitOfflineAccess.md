[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / serverScopesOmitOfflineAccess

# Function: serverScopesOmitOfflineAccess()

> **serverScopesOmitOfflineAccess**(`options`): [`OfflineAccessOmissionValidation`](../type-aliases/OfflineAccessOmissionValidation.md)

Defined in: [protocol/authorization-registration.ts:1248](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-registration.ts#L1248)

Validates that a server (protected resource) does NOT include `offline_access`
in its `WWW-Authenticate` `scope` or in its `scopes_supported`, as a server
SHOULD ensure — refresh tokens are not a resource requirement. (R-23.19-u)

## Parameters

### options

#### challengeScope?

`string`

The `WWW-Authenticate` `scope` the server emits, if any.

#### scopesSupported?

readonly `string`[]

The server's protected-resource `scopes_supported`, if any.

## Returns

[`OfflineAccessOmissionValidation`](../type-aliases/OfflineAccessOmissionValidation.md)
