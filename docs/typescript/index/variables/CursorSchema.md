[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CursorSchema

# Variable: CursorSchema

> `const` **CursorSchema**: `ZodString`

Defined in: [jsonrpc/payload.ts:200](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/jsonrpc/payload.ts#L200)

An opaque pagination token referenced by paginated methods. (§3.7)

Canonical type home: §3.7 (Appendix E). Use in list operations is defined
in §12 / S18. Receivers MUST NOT parse or infer structure from a cursor value.
(R-3.7-d)
