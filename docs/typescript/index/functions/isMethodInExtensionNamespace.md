[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isMethodInExtensionNamespace

# Function: isMethodInExtensionNamespace()

> **isMethodInExtensionNamespace**(`method`, `identifier`): `boolean`

Defined in: [protocol/extension-mechanism.ts:267](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extension-mechanism.ts#L267)

Returns `true` when `method` belongs to the namespace derived from
`identifier` — i.e. it begins with `<extension-name>/` and carries a non-empty
member segment after the slash. (R-24.5-b)

The member segment is the part after the namespace prefix; it MUST be
non-empty (`tasks/` alone is not a method) but is otherwise unconstrained here.

## Parameters

### method

`string`

### identifier

`string`

## Returns

`boolean`
