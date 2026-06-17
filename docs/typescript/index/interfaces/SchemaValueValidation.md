[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SchemaValueValidation

# Interface: SchemaValueValidation

Defined in: [protocol/tools.ts:434](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L434)

The outcome of validating a JSON value against a JSON Schema document.

## Properties

### valid

> **valid**: `boolean`

Defined in: [protocol/tools.ts:436](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L436)

`true` when `value` conforms to the schema.

***

### errors

> **errors**: `string`[]

Defined in: [protocol/tools.ts:438](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools.ts#L438)

Human-readable validation errors (empty when `valid`).
