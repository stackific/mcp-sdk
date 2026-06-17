[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / TitledMultiSelectItemsSchema

# Variable: TitledMultiSelectItemsSchema

> `const` **TitledMultiSelectItemsSchema**: `ZodObject`\<\{ `anyOf`: `ZodArray`\<`ZodObject`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>\>, `"many"`\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `anyOf`: `ZodArray`\<`ZodObject`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>\>, `"many"`\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `anyOf`: `ZodArray`\<`ZodObject`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `const`: `ZodString`; `title`: `ZodString`; \}, `ZodTypeAny`, `"passthrough"`\>\>, `"many"`\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/elicitation-form.ts:258](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L258)

The `items` schema of a titled multi-select enum: an `anyOf` of options. (§20.4)
