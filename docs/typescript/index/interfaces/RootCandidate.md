[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RootCandidate

# Interface: RootCandidate

Defined in: [protocol/roots.ts:449](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L449)

A candidate root a client is considering exposing, paired with consent state.

## Properties

### root

> **root**: `objectOutputType`

Defined in: [protocol/roots.ts:451](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L451)

The candidate root entry.

***

### consented

> **consented**: `boolean`

Defined in: [protocol/roots.ts:453](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L453)

Whether the user has consented to exposing this root. (R-21.1.5-h · SHOULD)

***

### inScope

> **inScope**: `boolean`

Defined in: [protocol/roots.ts:455](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L455)

Whether the client intends the server to treat this root as in-scope. (R-21.1.5-g · MUST)
