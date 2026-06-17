[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / UpdateTaskResultSchema

# Variable: UpdateTaskResultSchema

> `const` **UpdateTaskResultSchema**: `ZodObject`\<\{ `resultType`: `ZodLiteral`\<`"complete"`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `resultType`: `ZodLiteral`\<`"complete"`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `resultType`: `ZodLiteral`\<`"complete"`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>\> = `TaskAcknowledgementResultSchema`

Defined in: [protocol/tasks-lifecycle.ts:424](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tasks-lifecycle.ts#L424)

The `tasks/update` result: the empty `"complete"` acknowledgement. (§25.8, R-25.8-j, R-25.8-k)
