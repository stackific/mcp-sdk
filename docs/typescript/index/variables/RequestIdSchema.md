[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RequestIdSchema

# Variable: RequestIdSchema

> `const` **RequestIdSchema**: `ZodUnion`\<\[`ZodString`, `ZodEffects`\<`ZodNumber`, `number`, `number`\>\]\>

Defined in: [jsonrpc/framing.ts:23](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/jsonrpc/framing.ts#L23)

`RequestId` correlates a response with the request that originated it.

MUST be a JSON string or JSON number. MUST NOT be `null`. (R-3.2-a, R-3.2-b)
This is stricter than base JSON-RPC 2.0 which permits `null`.
