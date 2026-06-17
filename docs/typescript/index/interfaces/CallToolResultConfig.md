[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CallToolResultConfig

# Interface: CallToolResultConfig

Defined in: [protocol/tools-call.ts:293](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L293)

The server-supplied inputs to a successful (non-error) `CallToolResult`.

## Properties

### content

> **content**: readonly [`ContentBlock`](../type-aliases/ContentBlock.md)[]

Defined in: [protocol/tools-call.ts:295](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L295)

REQUIRED unstructured content blocks; MAY be empty / mixed. (R-16.5-l, R-16.5-m)

***

### structuredContent?

> `optional` **structuredContent?**: `unknown`

Defined in: [protocol/tools-call.ts:300](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L300)

OPTIONAL structured result (ANY JSON value). Pass the property to include it
— including an explicit `null`. (R-16.5-n)

***

### isError?

> `optional` **isError?**: `boolean`

Defined in: [protocol/tools-call.ts:302](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L302)

OPTIONAL; defaults to `false` (success) when omitted. (R-16.5-q)

***

### \_meta?

> `optional` **\_meta?**: `Record`\<`string`, `unknown`\>

Defined in: [protocol/tools-call.ts:304](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L304)

OPTIONAL reserved metadata map. (R-16.5-s)
