[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / BooleanSchemaSchema

# Variable: BooleanSchemaSchema

> `const` **BooleanSchemaSchema**: `ZodObject`\<\{ `type`: `ZodLiteral`\<`"boolean"`\>; `title`: `ZodOptional`\<`ZodString`\>; `description`: `ZodOptional`\<`ZodString`\>; `default`: `ZodOptional`\<`ZodBoolean`\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `type`: `ZodLiteral`\<`"boolean"`\>; `title`: `ZodOptional`\<`ZodString`\>; `description`: `ZodOptional`\<`ZodString`\>; `default`: `ZodOptional`\<`ZodBoolean`\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `type`: `ZodLiteral`\<`"boolean"`\>; `title`: `ZodOptional`\<`ZodString`\>; `description`: `ZodOptional`\<`ZodString`\>; `default`: `ZodOptional`\<`ZodBoolean`\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/elicitation-form.ts:142](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L142)

A true/false field of a form-mode `requestedSchema`. (§20.4)

  - `type` REQUIRED; MUST be the literal `"boolean"`.
  - `title` / `description` OPTIONAL display strings.
  - `default` OPTIONAL boolean pre-population value. (R-20.4-c)
