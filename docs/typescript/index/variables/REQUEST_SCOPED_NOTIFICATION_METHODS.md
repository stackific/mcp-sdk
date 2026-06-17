[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / REQUEST\_SCOPED\_NOTIFICATION\_METHODS

# Variable: REQUEST\_SCOPED\_NOTIFICATION\_METHODS

> `const` **REQUEST\_SCOPED\_NOTIFICATION\_METHODS**: readonly \[`"notifications/progress"`, `"notifications/message"`\]

Defined in: [protocol/streaming.ts:90](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L90)

The two request-scoped notification kinds that MUST travel on a request's own
response stream and MUST NOT appear on a subscription stream. (§10.6, R-10.6-b, R-10.6-e)

Reuses the canonical method-name constants from S22 (progress) and S23 (logging)
rather than re-typing the literals.
