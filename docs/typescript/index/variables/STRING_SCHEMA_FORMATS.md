[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / STRING\_SCHEMA\_FORMATS

# Variable: STRING\_SCHEMA\_FORMATS

> `const` **STRING\_SCHEMA\_FORMATS**: readonly \[`"email"`, `"uri"`, `"date"`, `"date-time"`\]

Defined in: [protocol/elicitation-form.ts:48](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L48)

The four permitted `StringSchema.format` literals. A `format`, when present,
MUST be exactly one of these; any other value (e.g. `"phone"`) is rejected.
(§20.4, R-20.4-d)
