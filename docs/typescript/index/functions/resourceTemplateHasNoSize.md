[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / resourceTemplateHasNoSize

# Function: resourceTemplateHasNoSize()

> **resourceTemplateHasNoSize**(`template`): `boolean`

Defined in: [protocol/resources.ts:403](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources.ts#L403)

Returns `true` when `template` carries no `size` field — a `ResourceTemplate`
MUST NOT have one (size belongs to a concrete resource, not a template).
(§17.4, R-17.4-u)

## Parameters

### template

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
