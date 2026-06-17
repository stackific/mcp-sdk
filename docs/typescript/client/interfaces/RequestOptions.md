[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / RequestOptions

# Interface: RequestOptions

Defined in: [client/client.ts:90](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L90)

Per-call options for [Client.request](../classes/Client.md#request) / [Client.callTool](../classes/Client.md#calltool).

## Properties

### signal?

> `optional` **signal?**: `AbortSignal`

Defined in: [client/client.ts:92](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L92)

Abort the request; sends `notifications/cancelled` and rejects locally. (§15.2)

***

### timeoutMs?

> `optional` **timeoutMs?**: `number`

Defined in: [client/client.ts:94](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L94)

Reject (and cancel) the request if no response arrives within this many ms.

***

### onProgress?

> `optional` **onProgress?**: [`ProgressHandler`](../type-aliases/ProgressHandler.md)

Defined in: [client/client.ts:96](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L96)

Receive correlated `notifications/progress` for this request. (§15.1)

***

### progressToken?

> `optional` **progressToken?**: `string` \| `number`

Defined in: [client/client.ts:98](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L98)

Explicit progress token; one is derived from the request id when omitted.
