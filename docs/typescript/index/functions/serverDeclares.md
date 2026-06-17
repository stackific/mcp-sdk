[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / serverDeclares

# Function: serverDeclares()

> **serverDeclares**(`caps`, `capability`): `boolean`

Defined in: [protocol/capability-negotiation.ts:210](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/capability-negotiation.ts#L210)

Returns `true` when the server's capabilities declare `capability`. (§6.2)

Object capabilities are declared by presence; the boolean sub-flags
(`listChanged`, `subscribe`) are declared only when explicitly `true`
(absent or `false` ⇒ not declared). (R-6.3-h, R-6.3-l, R-6.3-o)

## Parameters

### caps

`Record`\<`string`, `unknown`\>

### capability

[`ServerCapabilityName`](../type-aliases/ServerCapabilityName.md)

## Returns

`boolean`
