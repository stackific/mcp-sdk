[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / ToolDef

# Interface: ToolDef

Defined in: [server/server.ts:157](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L157)

## Properties

### title?

> `optional` **title?**: `string`

Defined in: [server/server.ts:158](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L158)

***

### description?

> `optional` **description?**: `string`

Defined in: [server/server.ts:159](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L159)

***

### inputSchema?

> `optional` **inputSchema?**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:161](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L161)

JSON Schema (2020-12) for `arguments`; validated by the SDK value validator.

***

### outputSchema?

> `optional` **outputSchema?**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:162](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L162)

***

### annotations?

> `optional` **annotations?**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:163](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L163)

***

### execution?

> `optional` **execution?**: `object`

Defined in: [server/server.ts:165](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L165)

Task-augmented tool: `{ taskSupport: 'required' | 'optional' }`.

#### taskSupport

> **taskSupport**: `"required"` \| `"optional"`

***

### \_meta?

> `optional` **\_meta?**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:171](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L171)

Reserved metadata published on this tool's `tools/list` entry — e.g.
`_meta.ui` to advertise an Interactive UI surface (S41/S42, §26). Emitted
verbatim so `McpServer` can advertise `_meta.ui` on a `tools/list` entry.
