[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / clientShouldExpectNotification

# Function: clientShouldExpectNotification()

> **clientShouldExpectNotification**(`notification`, `serverCaps`): `boolean`

Defined in: [protocol/capability-negotiation.ts:286](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/capability-negotiation.ts#L286)

Returns `true` when a client should expect `notification` given the server's
declared capabilities. When the gating sub-flag is absent or `false`, the
client MUST NOT expect the notification. (R-6.3-h, R-6.3-l, R-6.3-o)

## Parameters

### notification

`string`

### serverCaps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
