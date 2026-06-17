[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / declaresRoots

# Function: declaresRoots()

> **declaresRoots**(`caps`): `boolean`

Defined in: [protocol/roots.ts:156](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L156)

Returns `true` when the client-capabilities object `caps` declares the
(Deprecated) `roots` capability. (R-21.1.2-a; AC-32.4)

Thin wrapper over `clientDeclares(caps, 'roots')`: presence of an OBJECT
`roots` value means declared, even when it carries unrecognized members — the
capability is NOT rejected for those. (R-21.1.2-b · MUST)

## Parameters

### caps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
