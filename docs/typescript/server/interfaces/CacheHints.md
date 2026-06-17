[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / CacheHints

# Interface: CacheHints

Defined in: [server/caching.ts:15](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/caching.ts#L15)

Top-level caching hints on a cacheable result. (§13.1–§13.4)

## Properties

### ttlMs?

> `optional` **ttlMs?**: `number`

Defined in: [server/caching.ts:17](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/caching.ts#L17)

Freshness lifetime in ms; a client MAY reuse the cached result within it. (§13.2)

***

### cacheScope?

> `optional` **cacheScope?**: `"public"` \| `"private"`

Defined in: [server/caching.ts:19](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/caching.ts#L19)

Cache-sharing scope — exactly `"public"` or `"private"`. (§13.3)
