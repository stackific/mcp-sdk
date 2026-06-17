[**@stackific/mcp-sdk**](../../../README.md)

***

[@stackific/mcp-sdk](../../../README.md) / [server/node](../README.md) / createNodeHttpHandler

# Function: createNodeHttpHandler()

> **createNodeHttpHandler**(`server`, `options?`): (`req`, `res`) => `void`

Defined in: [server/node.ts:23](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/node.ts#L23)

Builds a `node:http` request listener that serves `server` over Streamable HTTP.

## Parameters

### server

[`McpServer`](../../classes/McpServer.md)

### options?

[`McpRequestHandlerOptions`](../../interfaces/McpRequestHandlerOptions.md) = `{}`

## Returns

(`req`, `res`) => `void`

## Example

```ts
import { createServer } from 'node:http';
createServer(createNodeHttpHandler(server)).listen(7001);
```
