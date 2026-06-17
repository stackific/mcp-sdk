[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildResourcesCapability

# Function: buildResourcesCapability()

> **buildResourcesCapability**(`opts?`): `objectOutputType`

Defined in: [protocol/resources.ts:593](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources.ts#L593)

Builds the `resources` value for a server's `ServerCapabilities`, including a
sub-flag only when explicitly `true`. The empty-object form `{}` (neither
sub-flag) is produced when both are omitted/false. (§17.1, R-17.1-f, R-17.1-g)

## Parameters

### opts?

OPTIONAL `listChanged` / `subscribe` sub-flags.

#### listChanged?

`boolean`

#### subscribe?

`boolean`

## Returns

`objectOutputType`
