[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RestrictedFormSchemaError

# Interface: RestrictedFormSchemaError

Defined in: [protocol/elicitation-form.ts:462](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L462)

One failure reported by [validateRestrictedFormSchema](../functions/validateRestrictedFormSchema.md).

## Properties

### path

> **path**: `string`

Defined in: [protocol/elicitation-form.ts:464](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L464)

A dotted path to the offending node (e.g. `properties.age`).

***

### detail

> **detail**: `string`

Defined in: [protocol/elicitation-form.ts:466](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L466)

Human-readable detail.
