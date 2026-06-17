[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / toolDisplayName

# Function: toolDisplayName()

> **toolDisplayName**(`tool`): `string`

Defined in: [protocol/tools.ts:603](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L603)

Resolves the display name to show for a tool, applying the §16.3 precedence:
`title` → `annotations.title` → `name`. (R-16.3-i; reuses S20
`resolveDisplayName`.)

## Parameters

### tool

#### name

`string`

#### title?

`string`

#### annotations?

\{ `title?`: `string`; \}

#### annotations.title?

`string`

## Returns

`string`
