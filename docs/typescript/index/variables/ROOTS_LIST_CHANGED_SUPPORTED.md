[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ROOTS\_LIST\_CHANGED\_SUPPORTED

# Variable: ROOTS\_LIST\_CHANGED\_SUPPORTED

> `const` **ROOTS\_LIST\_CHANGED\_SUPPORTED**: `false`

Defined in: [protocol/roots.ts:171](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L171)

`false` — this revision defines NO `listChanged` sub-flag for the `roots`
capability. (R-21.1.2-c · MUST NOT; AC-32.5)

A client MUST NOT rely on any `listChanged`-style change-notification
mechanism for roots in this revision. The notification method name and a
predicate that confirms it is unsupported are provided below so callers can
assert (rather than assume) its absence.
