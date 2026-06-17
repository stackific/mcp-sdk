[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ResourceNotFoundError

# Interface: ResourceNotFoundError

Defined in: [protocol/resources-read.ts:126](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L126)

A JSON-RPC resource-not-found error payload. (§17.6)

## Properties

### code

> **code**: `-32602`

Defined in: [protocol/resources-read.ts:128](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L128)

`-32602` (Invalid params). (R-17.6-a)

***

### message

> **message**: `string`

Defined in: [protocol/resources-read.ts:130](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L130)

Human-readable description, e.g. "Resource not found".

***

### data

> **data**: [`ResourceNotFoundErrorData`](ResourceNotFoundErrorData.md)

Defined in: [protocol/resources-read.ts:132](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L132)

SHOULD include the offending `uri`. (R-17.6-b)
