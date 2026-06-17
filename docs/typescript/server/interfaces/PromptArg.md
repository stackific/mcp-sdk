[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / PromptArg

# Interface: PromptArg

Defined in: [server/server.ts:192](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L192)

## Properties

### name

> **name**: `string`

Defined in: [server/server.ts:193](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L193)

***

### description?

> `optional` **description?**: `string`

Defined in: [server/server.ts:194](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L194)

***

### required?

> `optional` **required?**: `boolean`

Defined in: [server/server.ts:195](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L195)

***

### complete?

> `optional` **complete?**: (`value`) => `string`[]

Defined in: [server/server.ts:197](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L197)

Completion callback for this argument.

#### Parameters

##### value

`string`

#### Returns

`string`[]
