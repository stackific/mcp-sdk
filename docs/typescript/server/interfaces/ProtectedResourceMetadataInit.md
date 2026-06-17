[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / ProtectedResourceMetadataInit

# Interface: ProtectedResourceMetadataInit

Defined in: [server/auth.ts:23](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/auth.ts#L23)

Inputs to [buildProtectedResourceMetadata](../functions/buildProtectedResourceMetadata.md).

## Properties

### resource

> **resource**: `string`

Defined in: [server/auth.ts:25](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/auth.ts#L25)

The canonical resource identifier (the MCP endpoint URL).

***

### authorizationServers

> **authorizationServers**: `string`[]

Defined in: [server/auth.ts:27](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/auth.ts#L27)

The authorization server issuer URLs that protect this resource.

***

### scopes?

> `optional` **scopes?**: `string`[]

Defined in: [server/auth.ts:29](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/auth.ts#L29)

OPTIONAL scopes the resource recognizes.

***

### bearerMethods?

> `optional` **bearerMethods?**: `string`[]

Defined in: [server/auth.ts:31](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/auth.ts#L31)

OPTIONAL supported bearer-token delivery methods (default `['header']`).
