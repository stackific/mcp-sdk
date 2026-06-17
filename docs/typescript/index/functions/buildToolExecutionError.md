[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildToolExecutionError

# Function: buildToolExecutionError()

> **buildToolExecutionError**(`message`, `extra?`): `objectOutputType`

Defined in: [protocol/tools-call.ts:364](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L364)

Builds a *tool execution error* result: a successful `CallToolResult` (a
JSON-RPC result, NOT a JSON-RPC error) with `isError: true` and a human- and
model-readable explanation in `content`. This is the §16.6 mechanism for a
tool that reached execution and failed (upstream failure, semantically-invalid
input, business-logic failure), reported so the model can observe it and
self-correct. (§16.6, R-16.6-b)

## Parameters

### message

`string`

A human- and model-readable explanation of the failure.

### extra?

OPTIONAL extra content blocks / `structuredContent` / `_meta`.

#### content?

readonly [`ContentBlock`](../type-aliases/ContentBlock.md)[]

#### structuredContent?

`unknown`

#### _meta?

`Record`\<`string`, `unknown`\>

## Returns

`objectOutputType`
