[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildListResourceTemplatesResult

# Function: buildListResourceTemplatesResult()

> **buildListResourceTemplatesResult**(`resourceTemplates`, `hints`, `opts?`): `object` & `object` & `object` & `object`

Defined in: [protocol/resources.ts:565](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources.ts#L565)

Builds a `ListResourceTemplatesResult` with `resultType: "complete"` and the
REQUIRED caching hints; `nextCursor` / `_meta` included only when supplied.
(§17.3, R-17.3-b, R-17.3-c)

## Parameters

### resourceTemplates

readonly `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\>[]

### hints

[`ListCacheHints`](../interfaces/ListCacheHints.md)

### opts?

#### nextCursor?

`string`

#### _meta?

`Record`\<`string`, `unknown`\>

## Returns

## Throws

When `hints.ttlMs` is negative.
