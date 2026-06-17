[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RequestedSchemaError

# Interface: RequestedSchemaError

Defined in: [protocol/elicitation.ts:285](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation.ts#L285)

One failure reported by [validateRequestedSchema](../functions/validateRequestedSchema.md).

## Properties

### path

> **path**: `string`

Defined in: [protocol/elicitation.ts:287](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation.ts#L287)

A dotted path to the offending node (e.g. `properties.address`).

***

### detail

> **detail**: `string`

Defined in: [protocol/elicitation.ts:289](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation.ts#L289)

Human-readable detail.
