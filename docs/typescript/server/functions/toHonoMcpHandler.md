[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / toHonoMcpHandler

# Function: toHonoMcpHandler()

> **toHonoMcpHandler**(`server`, `options?`): (`c`) => `Promise`\<`Response`\>

Defined in: [server/hono.ts:26](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/hono.ts#L26)

Builds a Hono route handler that serves `server` over Streamable HTTP.

## Parameters

### server

[`McpServer`](../classes/McpServer.md)

### options?

[`McpRequestHandlerOptions`](../interfaces/McpRequestHandlerOptions.md) = `{}`

## Returns

(`c`) => `Promise`\<`Response`\>

## Example

```ts
import { Hono } from 'hono';
const app = new Hono();
app.all('/mcp', toHonoMcpHandler(server));
export default app; // Workers/Deno/Bun; or serve() it on Node
```
