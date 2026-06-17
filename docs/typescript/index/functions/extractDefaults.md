[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / extractDefaults

# Function: extractDefaults()

> **extractDefaults**(`requestedSchema`): `Record`\<`string`, `string` \| `number` \| `boolean` \| `string`[]\>

Defined in: [protocol/elicitation-form.ts:553](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L553)

Extracts the per-field `default` values declared in a restricted form schema,
so a defaults-supporting client can pre-populate the corresponding fields.
(§20.4, R-20.4-c)

Returns a map from field name to its declared `default`, including only the
fields that declare one. The value is returned as-is (string, number, boolean,
or `string[]` per the field's primitive type). A client that supports defaults
SHOULD use these to pre-populate; a client that does not MAY ignore them.

## Parameters

### requestedSchema

`unknown`

A form-mode `requestedSchema`.

## Returns

`Record`\<`string`, `string` \| `number` \| `boolean` \| `string`[]\>
