[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / classifyEnumSchema

# Function: classifyEnumSchema()

> **classifyEnumSchema**(`value`): [`EnumSchemaForm`](../type-aliases/EnumSchemaForm.md) \| `undefined`

Defined in: [protocol/elicitation-form.ts:366](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L366)

Classifies an enum schema into one of its five structural forms by the
distinguishing keyword, or returns `undefined` when `value` is not a
well-formed enum schema. (§20.4)

Classification order resolves overlaps:
  - `type: "array"` ⇒ multi-select; `items.anyOf` ⇒ titled, `items.enum` ⇒ untitled.
  - `type: "string"` with `oneOf` ⇒ titled single-select.
  - `type: "string"` with `enum` + `enumNames` ⇒ legacy titled.
  - `type: "string"` with `enum` (no `enumNames`) ⇒ untitled single-select.

`enumNames` is the deciding marker for the Deprecated legacy form; an untitled
single-select carries `enum` without it. (R-20.4-f)

## Parameters

### value

`unknown`

## Returns

[`EnumSchemaForm`](../type-aliases/EnumSchemaForm.md) \| `undefined`
