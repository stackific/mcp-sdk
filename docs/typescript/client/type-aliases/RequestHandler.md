[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / RequestHandler

# Type Alias: RequestHandler

> **RequestHandler** = (`params`, `extra`) => `unknown` \| `Promise`\<`unknown`\>

Defined in: [client/client.ts:78](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L78)

Handles an inbound server→client request (e.g. `sampling/createMessage`,
`elicitation/create`, `roots/list`). The returned object becomes the JSON-RPC
`result`; throwing a [RequestError](../classes/RequestError.md) maps to a JSON-RPC error response.

## Parameters

### params

`Record`\<`string`, `unknown`\>

### extra

#### id

[`RequestId`](../../index/type-aliases/RequestId.md)

#### method

`string`

## Returns

`unknown` \| `Promise`\<`unknown`\>
