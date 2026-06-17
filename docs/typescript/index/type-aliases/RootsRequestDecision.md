[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RootsRequestDecision

# Type Alias: RootsRequestDecision

> **RootsRequestDecision** = \{ `action`: `"request"`; \} \| \{ `action`: `"proceed-without-roots"`; \}

Defined in: [protocol/roots.ts:201](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L201)

Outcome of [decideRootsRequest](../functions/decideRootsRequest.md).

## Union Members

### Type Literal

\{ `action`: `"request"`; \}

The client declared `roots`; the server MAY embed a `roots/list` input request.

***

### Type Literal

\{ `action`: `"proceed-without-roots"`; \}

The client did NOT declare `roots`; the server MUST proceed without roots.
