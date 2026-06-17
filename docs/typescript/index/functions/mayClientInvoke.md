[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayClientInvoke

# Function: mayClientInvoke()

> **mayClientInvoke**(`method`, `serverCaps`): `boolean`

Defined in: [protocol/capability-negotiation.ts:262](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/capability-negotiation.ts#L262)

Returns `true` when a client MAY invoke `method` given the server's declared
capabilities. (R-6.3-e, R-6.4-f, R-6.4-g)

An ungated (core) method is always invocable; a gated method requires the
governing capability to be declared in `serverCaps`.

## Parameters

### method

`string`

### serverCaps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
