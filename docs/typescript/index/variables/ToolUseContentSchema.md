[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ToolUseContentSchema

# Variable: ToolUseContentSchema

> `const` **ToolUseContentSchema**: `ZodObject`\<\{ `type`: `ZodLiteral`\<`"tool_use"`\>; `id`: `ZodString`; `name`: `ZodString`; `input`: `ZodRecord`\<`ZodString`, `ZodUnknown`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `type`: `ZodLiteral`\<`"tool_use"`\>; `id`: `ZodString`; `name`: `ZodString`; `input`: `ZodRecord`\<`ZodString`, `ZodUnknown`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `type`: `ZodLiteral`\<`"tool_use"`\>; `id`: `ZodString`; `name`: `ZodString`; `input`: `ZodRecord`\<`ZodString`, `ZodUnknown`\>; `_meta`: `ZodOptional`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/sampling.ts:101](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L101)

`ToolUseContent` — a request from the assistant to call a tool. (§21.2.6)

Only valid inside sampling messages/results; MUST NOT appear where a base
`ContentBlock` is expected (S14 forbids `tool_use`/`tool_result` there).

Fields: `type` literal `"tool_use"`; `id` (unique, matches results to uses);
`name`; `input` object; OPTIONAL `_meta` which clients SHOULD preserve across
subsequent sampling requests for caching (S19). (R-21.2.6-c)
