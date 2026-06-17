[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isHttpsResourceUri

# Function: isHttpsResourceUri()

> **isHttpsResourceUri**(`value`): `boolean`

Defined in: [protocol/resources-read.ts:593](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L593)

Returns `true` when `value` is an `https`-scheme resource URI — the case in
which a client MAY fetch the resource directly from the web rather than via
`resources/read`. (§17.5, §17.9, R-17.5-y, R-17.9-b)

## Parameters

### value

`unknown`

## Returns

`boolean`
