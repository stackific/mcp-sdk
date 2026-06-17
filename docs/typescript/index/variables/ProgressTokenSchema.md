[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ProgressTokenSchema

# Variable: ProgressTokenSchema

> `const` **ProgressTokenSchema**: `ZodUnion`\<\[`ZodString`, `ZodNumber`\]\>

Defined in: [jsonrpc/payload.ts:188](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/jsonrpc/payload.ts#L188)

An opaque value the requester places in request `_meta` to correlate
out-of-band progress notifications to the request. (§3.7, §15.1)

Canonical type home: §3.7 (Appendix E). Placement within `_meta` and the
full progress-notification flow are defined in §15 / S22.

The receiver MAY emit correlated progress notifications but is not obligated
to do so. (R-3.7-c)
