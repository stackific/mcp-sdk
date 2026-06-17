[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / McpServerOptions

# Interface: McpServerOptions

Defined in: [server/server.ts:242](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L242)

Options for [McpServer](../classes/McpServer.md).

## Properties

### pageSize?

> `optional` **pageSize?**: `number`

Defined in: [server/server.ts:244](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L244)

Max items per page for the list methods (tools/resources/prompts). Default 50. (§12)

***

### cacheTtlMs?

> `optional` **cacheTtlMs?**: `number`

Defined in: [server/server.ts:250](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L250)

Default freshness hint (ms) stamped as the top-level `ttlMs` on the five
cacheable-method results (§13.4); default `0` (a non-caching server still
MUST emit the field). (R-13.4-b)

***

### cacheScope?

> `optional` **cacheScope?**: `"public"` \| `"private"`

Defined in: [server/server.ts:252](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L252)

Default top-level `cacheScope` for cacheable results; default `'private'`. (§13.3)

***

### toolCallRateLimit?

> `optional` **toolCallRateLimit?**: `object`

Defined in: [server/server.ts:259](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L259)

Opt-in §28.3 rate limit for `tools/call`. When set, the server rejects a call
that exceeds `maxInWindow` invocations per `windowMs` (per caller identity)
with a `-32600` error rather than executing it. (R-28.3-g, R-28.3-h) Omitted
⇒ no rate limiting (back-compatible default; the embedder owns the policy).

#### maxInWindow

> **maxInWindow**: `number`

#### windowMs

> **windowMs**: `number`

***

### resourceAccess?

> `optional` **resourceAccess?**: `object`

Defined in: [server/server.ts:266](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L266)

Opt-in §28.10 resource-access policy. When set, a `resources/read` URI is
validated BEFORE it is dereferenced: it MUST parse as an absolute URI
(R-28.10-f), resolve to a location `isAuthorizedLocation` permits (R-28.10-g),
and — with `guardSsrf` — not target a private/loopback host (R-28.10-h).

#### isAuthorizedLocation

> **isAuthorizedLocation**: (`url`) => `boolean`

##### Parameters

###### url

`URL`

##### Returns

`boolean`

#### guardSsrf?

> `optional` **guardSsrf?**: `boolean`

***

### fileResourceRoot?

> `optional` **fileResourceRoot?**: `string`

Defined in: [server/server.ts:273](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L273)

Opt-in §28.10 authorized `file://` root. When set, the path of a `file://`
`resources/read` URI is sanitized against this root and directory traversal
(`..`, NUL bytes, absolute escapes) is rejected BEFORE any reader runs.
(R-28.10-o, R-28.10-p)
