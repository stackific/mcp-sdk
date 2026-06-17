[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / applicationTypeFor

# Function: applicationTypeFor()

> **applicationTypeFor**(`isNative`): [`ApplicationType`](../type-aliases/ApplicationType.md)

Defined in: [protocol/authorization-flow.ts:404](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L404)

Returns the `application_type` a client SHOULD register based on whether it runs
as a native (desktop/mobile/CLI/localhost) or a remote browser-based app.
(R-23.4-n, R-23.4-o)

## Parameters

### isNative

`boolean`

`true` for desktop/mobile/CLI/localhost-hosted clients.

## Returns

[`ApplicationType`](../type-aliases/ApplicationType.md)
