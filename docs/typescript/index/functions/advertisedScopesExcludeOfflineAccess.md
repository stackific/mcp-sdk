[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / advertisedScopesExcludeOfflineAccess

# Function: advertisedScopesExcludeOfflineAccess()

> **advertisedScopesExcludeOfflineAccess**(`options`): `boolean`

Defined in: [protocol/authorization-flow.ts:740](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L740)

Returns `true` when neither the `WWW-Authenticate` `scope` nor protected-resource
`scopes_supported` includes `offline_access`, as an MCP server SHOULD ensure.
(R-23.9-g)

## Parameters

### options

#### challengeScope?

`string`

The `WWW-Authenticate` `scope` value, if any.

#### scopesSupported?

`string`[]

Protected-resource `scopes_supported`, if any.

## Returns

`boolean`
