[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / TaskTtlMsSchema

# Variable: TaskTtlMsSchema

> `const` **TaskTtlMsSchema**: `ZodUnion`\<\[`ZodNumber`, `ZodNull`\]\>

Defined in: [protocol/tasks.ts:312](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tasks.ts#L312)

Schema for `ttlMs`: a non-negative number, or `null` (unbounded). (§25.4,
R-25.4-b) After a non-null value elapses, a server MAY discard the task.
