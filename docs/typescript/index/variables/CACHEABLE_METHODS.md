[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CACHEABLE\_METHODS

# Variable: CACHEABLE\_METHODS

> `const` **CACHEABLE\_METHODS**: `Set`\<`"prompts/list"` \| `"resources/list"` \| `"resources/read"` \| `"tools/list"` \| `"resources/templates/list"`\>

Defined in: [protocol/caching.ts:300](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/caching.ts#L300)

The set of method names whose results carry `CacheableResult` shapes. (§13.4, R-13.4-a)

On every result from these methods a server MUST populate both `ttlMs` and
`cacheScope` with valid values. A server that does not wish to encourage
caching MUST still include the fields and SHOULD set `ttlMs` to `0`. (R-13.4-b)

On any other message, receivers MUST ignore `ttlMs`/`cacheScope` if present.
(R-13.4-e)
