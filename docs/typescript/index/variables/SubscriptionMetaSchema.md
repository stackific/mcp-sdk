[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SubscriptionMetaSchema

# Variable: SubscriptionMetaSchema

> `const` **SubscriptionMetaSchema**: `ZodIntersection`\<`ZodRecord`\<`ZodString`, `ZodUnknown`\>, `ZodObject`\<\{ `io.modelcontextprotocol/subscriptionId`: `ZodString`; \}, `"strip"`, `ZodTypeAny`, \{ `io.modelcontextprotocol/subscriptionId`: `string`; \}, \{ `io.modelcontextprotocol/subscriptionId`: `string`; \}\>\>

Defined in: [protocol/streaming.ts:248](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L248)

The `_meta` fragment present on every subscription notification: it MUST contain
the reserved `io.modelcontextprotocol/subscriptionId` string key. (§10.4, R-10.4-a)

`.passthrough()` preserves any other `_meta` members. The schema requires the
reserved key to be a string (the request `id` serialized as a JSON string).
