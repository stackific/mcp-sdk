[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ElicitContentSchema

# Variable: ElicitContentSchema

> `const` **ElicitContentSchema**: `ZodRecord`\<`ZodString`, `ZodUnion`\<\[`ZodString`, `ZodNumber`, `ZodBoolean`, `ZodArray`\<`ZodString`, `"many"`\>\]\>\>

Defined in: [protocol/elicitation-form.ts:612](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L612)

Schema for the `ElicitResult.content` map: field name → permitted value type.
(§20.5, R-20.5-c)
