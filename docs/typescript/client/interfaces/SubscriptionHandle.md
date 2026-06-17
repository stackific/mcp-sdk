[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / SubscriptionHandle

# Interface: SubscriptionHandle

Defined in: [client/client.ts:142](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L142)

A handle to an active subscription opened via [Client.subscribe](../classes/Client.md#subscribe). (§10)

## Properties

### subscriptionId

> **subscriptionId**: `string`

Defined in: [client/client.ts:144](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L144)

The server-assigned subscription id (`io.modelcontextprotocol/subscriptionId`).

***

### acknowledgedFilter

> **acknowledgedFilter**: `Record`\<`string`, `unknown`\>

Defined in: [client/client.ts:146](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L146)

The honored subset of the requested filter, from the acknowledgement.

***

### closed

> **closed**: `Promise`\<`void`\>

Defined in: [client/client.ts:148](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L148)

Resolves when the subscription stream ends (teardown / unsubscribe / disconnect).

## Methods

### unsubscribe()

> **unsubscribe**(): `Promise`\<`void`\>

Defined in: [client/client.ts:150](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L150)

Tears the subscription down (sends `notifications/cancelled` for the listen request).

#### Returns

`Promise`\<`void`\>
