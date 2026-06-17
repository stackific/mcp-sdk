[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ElicitContentError

# Interface: ElicitContentError

Defined in: [protocol/elicitation-form.ts:641](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L641)

One failure reported by [validateElicitContent](../functions/validateElicitContent.md).

## Properties

### path

> **path**: `string`

Defined in: [protocol/elicitation-form.ts:643](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L643)

The offending field name, or `<root>` for a top-level shape problem.

***

### detail

> **detail**: `string`

Defined in: [protocol/elicitation-form.ts:645](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L645)

Human-readable detail.
