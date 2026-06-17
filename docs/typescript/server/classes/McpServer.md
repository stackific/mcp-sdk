[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / McpServer

# Class: McpServer

Defined in: [server/server.ts:282](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L282)

Server runtime — an embeddable, edge-friendly MCP server: the `McpServer`
dispatcher + registration API, a Web-standard Streamable HTTP request handler,
and a Hono adapter.

This barrel imports no `node:*` and uses only Web-platform APIs, so it can be
imported on Cloudflare Workers / Deno / Bun as well as Node. Import it via the
package's `./server` subpath. The Node (`node:http`) adapter is kept separate
under `./server/node` so it never enters an edge bundle.

## Constructors

### Constructor

> **new McpServer**(`info`, `capabilities?`, `options?`): `McpServer`

Defined in: [server/server.ts:300](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L300)

#### Parameters

##### info

`objectOutputType`

##### capabilities?

`Record`\<`string`, `unknown`\> = `{}`

##### options?

[`McpServerOptions`](../interfaces/McpServerOptions.md) = `{}`

#### Returns

`McpServer`

## Properties

### info

> `readonly` **info**: `objectOutputType`

Defined in: [server/server.ts:301](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L301)

***

### capabilities

> `readonly` **capabilities**: `Record`\<`string`, `unknown`\> = `{}`

Defined in: [server/server.ts:302](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L302)

## Methods

### setTaskStore()

> **setTaskStore**(`store`): `void`

Defined in: [server/server.ts:348](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L348)

#### Parameters

##### store

[`TaskStore`](../interfaces/TaskStore.md)

#### Returns

`void`

***

### setTaskNotifier()

> **setTaskNotifier**(`notify`): `void`

Defined in: [server/server.ts:359](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L359)

Wires the subscription fan-out used to deliver `notifications/tasks` pushes
(§25.10). The Streamable HTTP handler calls this with its subscriber notifier;
a transport without subscriptions leaves it unset (push is then a no-op).

#### Parameters

##### notify

(`notification`) => `void`

#### Returns

`void`

***

### registerTool()

> **registerTool**(`name`, `def`, `handler`): `void`

Defined in: [server/server.ts:368](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L368)

#### Parameters

##### name

`string`

##### def

[`ToolDef`](../interfaces/ToolDef.md)

##### handler

[`ToolHandler`](../type-aliases/ToolHandler.md)

#### Returns

`void`

***

### registerResource()

> **registerResource**(`name`, `uri`, `def`, `read`): `void`

Defined in: [server/server.ts:371](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L371)

#### Parameters

##### name

`string`

##### uri

`string`

##### def

[`ResourceDef`](../interfaces/ResourceDef.md)

##### read

[`ResourceReader`](../type-aliases/ResourceReader.md)

#### Returns

`void`

***

### registerResourceTemplate()

> **registerResourceTemplate**(`name`, `def`, `read`): `void`

Defined in: [server/server.ts:374](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L374)

#### Parameters

##### name

`string`

##### def

[`ResourceTemplateDef`](../interfaces/ResourceTemplateDef.md)

##### read

[`TemplateReader`](../type-aliases/TemplateReader.md)

#### Returns

`void`

***

### registerPrompt()

> **registerPrompt**(`name`, `def`, `handler`): `void`

Defined in: [server/server.ts:377](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L377)

#### Parameters

##### name

`string`

##### def

[`PromptDef`](../interfaces/PromptDef.md)

##### handler

[`PromptHandler`](../type-aliases/PromptHandler.md)

#### Returns

`void`

***

### hasTool()

> **hasTool**(`name`): `boolean`

Defined in: [server/server.ts:380](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L380)

#### Parameters

##### name

`string`

#### Returns

`boolean`

***

### toolInputSchema()

> **toolInputSchema**(`name`): `Record`\<`string`, `unknown`\> \| `undefined`

Defined in: [server/server.ts:389](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L389)

Returns the registered `inputSchema` for tool `name`, or `undefined`. The
Streamable HTTP handler uses it to validate a `tools/call`'s `Mcp-Param-*`
headers against the body before dispatch (§9.5.4, S14/S15).

#### Parameters

##### name

`string`

#### Returns

`Record`\<`string`, `unknown`\> \| `undefined`

***

### dispatch()

> **dispatch**(`method`, `params`, `ctx`): `Promise`\<`Record`\<`string`, `unknown`\>\>

Defined in: [server/server.ts:406](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L406)

Routes one JSON-RPC request to its handler, returning the `result` payload.

#### Parameters

##### method

`string`

##### params

`Record`\<`string`, `unknown`\>

##### ctx

[`RequestContext`](../interfaces/RequestContext.md)

#### Returns

`Promise`\<`Record`\<`string`, `unknown`\>\>
