[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / classifyPrimitiveSchema

# Function: classifyPrimitiveSchema()

> **classifyPrimitiveSchema**(`value`): [`PrimitiveSchemaKind`](../type-aliases/PrimitiveSchemaKind.md) \| `undefined`

Defined in: [protocol/elicitation-form.ts:432](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L432)

Classifies a property schema by the `PrimitiveSchemaDefinition` member it
selects, or returns `undefined` when it is not a valid primitive schema.
(§20.4)

Selection is structural (per §20.4's table): `boolean` by `type`; `number` for
`"number"`/`"integer"`; `enum` for a string/array schema carrying
`enum`/`oneOf`/`items`; otherwise `string` for a plain `"string"`.

## Parameters

### value

`unknown`

## Returns

[`PrimitiveSchemaKind`](../type-aliases/PrimitiveSchemaKind.md) \| `undefined`
