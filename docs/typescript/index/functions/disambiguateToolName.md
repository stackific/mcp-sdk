[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / disambiguateToolName

# Function: disambiguateToolName()

> **disambiguateToolName**(`serverId`, `name`, `separator?`): `string`

Defined in: [protocol/tools.ts:635](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L635)

Applies a disambiguation strategy for an aggregated tool name: prefixes the
tool `name` with a server identifier (e.g. `server.tool`). A client or proxy
that encounters a name collision SHOULD apply such a strategy. (R-16.3-h)

## Parameters

### serverId

`string`

The server identifier to prefix with.

### name

`string`

The tool's original name.

### separator?

`string` = `'.'`

The prefix separator (default `'.'`, a permitted name char).

## Returns

`string`
