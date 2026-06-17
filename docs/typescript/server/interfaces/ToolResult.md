[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / ToolResult

# Interface: ToolResult

Defined in: [server/server.ts:108](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L108)

A tool result (standard MCP shape). `isError: true` reports a TOOL failure, not a protocol error.

## Properties

### content?

> `optional` **content?**: `unknown`[]

Defined in: [server/server.ts:109](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L109)

***

### structuredContent?

> `optional` **structuredContent?**: `unknown`

Defined in: [server/server.ts:110](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L110)

***

### isError?

> `optional` **isError?**: `boolean`

Defined in: [server/server.ts:111](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L111)

***

### \_meta?

> `optional` **\_meta?**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:112](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L112)

***

### task?

> `optional` **task?**: `unknown`

Defined in: [server/server.ts:114](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L114)

Present when a task-augmented call returns a handle instead of a result.
