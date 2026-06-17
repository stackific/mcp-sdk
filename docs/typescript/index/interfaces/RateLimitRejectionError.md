[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RateLimitRejectionError

# Interface: RateLimitRejectionError

Defined in: [protocol/security.ts:523](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L523)

A §28.3 rate-limit rejection error object, matching the story's wire example.

## Properties

### code

> **code**: `-32600`

Defined in: [protocol/security.ts:524](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L524)

***

### message

> **message**: `string`

Defined in: [protocol/security.ts:525](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L525)

***

### data?

> `optional` **data?**: `object`

Defined in: [protocol/security.ts:526](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L526)

#### retryAfterMs?

> `optional` **retryAfterMs?**: `number`
