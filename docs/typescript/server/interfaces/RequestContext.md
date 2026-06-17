[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / RequestContext

# Interface: RequestContext

Defined in: [server/server.ts:84](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L84)

Per-request context the transport hands the dispatcher (one per request, stateless).

## Properties

### protocolVersion

> **protocolVersion**: `string`

Defined in: [server/server.ts:86](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L86)

The negotiated protocol revision for this exchange.

***

### requestId

> **requestId**: `string` \| `number`

Defined in: [server/server.ts:88](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L88)

The JSON-RPC id of the originating request.

***

### meta

> **meta**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:90](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L90)

The request's `params._meta` (carries `progressToken`, trace context, …).

***

### signal

> **signal**: `AbortSignal`

Defined in: [server/server.ts:92](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L92)

Aborts when the client cancels this request (`notifications/cancelled`).

***

### authInfo?

> `optional` **authInfo?**: `unknown`

Defined in: [server/server.ts:94](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L94)

Transport-resolved caller identity (e.g. a validated bearer token), if any.

## Methods

### notify()

> **notify**(`notification`): `void`

Defined in: [server/server.ts:96](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L96)

Emits a notification on this request's stream.

#### Parameters

##### notification

###### method

`string`

###### params?

`Record`\<`string`, `unknown`\>

#### Returns

`void`

***

### serverRequest()

> **serverRequest**(`method`, `params`): `Promise`\<`Record`\<`string`, `unknown`\>\>

Defined in: [server/server.ts:98](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L98)

Issues a server→client request on this stream; resolves with the client's result.

#### Parameters

##### method

`string`

##### params

`Record`\<`string`, `unknown`\>

#### Returns

`Promise`\<`Record`\<`string`, `unknown`\>\>

***

### notifySubscribers()?

> `optional` **notifySubscribers**(`notification`): `void`

Defined in: [server/server.ts:104](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L104)

Broadcasts a change notification to active subscription streams, filtered by
each subscription's honored set (§10.5/§10.6). Optional — present only on
transports that support subscriptions (Streamable HTTP).

#### Parameters

##### notification

###### method

`string`

###### params?

`Record`\<`string`, `unknown`\>

#### Returns

`void`
