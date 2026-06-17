[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / createMcpRequestHandler

# Function: createMcpRequestHandler()

> **createMcpRequestHandler**(`server`, `options?`): (`request`) => `Promise`\<`Response`\>

Defined in: [server/streamable-http.ts:97](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/streamable-http.ts#L97)

Builds a Web `fetch` handler that serves `server` over Streamable HTTP.

Scope (S15): this is an **endpoint** server handler, not an intermediary or a
multi-host gateway. The §9 Recommended behaviors for intermediaries — version-
trust propagation and dual-hosting/multi-host guidance (RC-4/RC-5/RC-10/RC-11) —
do not apply to an endpoint and are intentionally out of scope here; an embedder
that fronts this handler with a proxy owns those intermediary obligations.

## Parameters

### server

[`McpServer`](../classes/McpServer.md)

### options?

[`McpRequestHandlerOptions`](../interfaces/McpRequestHandlerOptions.md) = `{}`

## Returns

(`request`) => `Promise`\<`Response`\>

## Example

```ts
// Cloudflare Workers
const handle = createMcpRequestHandler(server);
export default { fetch: (req: Request) => handle(req) };
```
