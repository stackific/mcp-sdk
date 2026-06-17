# Caching class

The §13 response-caching behavioral layer — the C# counterpart of the TypeScript `protocol/caching.ts` utilities. The wire records ([`CacheScope`](./CacheScope.md), [`CacheHints`](./CacheHints.md)) carry the advisory hints; this static class adds the runtime rules the spec layers on top: hint validation ([`IsCacheHintValid`](./Caching/IsCacheHintValid.md)), the privacy-default scope resolution ([`ResolveCacheScope`](./Caching/ResolveCacheScope.md), R-13.1-e), the client-local freshness computation ([`IsFresh`](./Caching/IsFresh.md), R-13.2), cross-page scope consistency (R-13.5-h), and the method→notification invalidation map (R-13.5-j). The cache itself is [`ResponseCache`](./ResponseCache-1.md).

```csharp
public static class Caching
```

## Public Members

| name | description |
| --- | --- |
| static [CacheableMethods](Caching/CacheableMethods.md) { get; } | The five method names whose results carry caching hints (§13.4, R-13.4-a). On every result from these methods a server MUST populate both `ttlMs` and `cacheScope`; on any other message receivers MUST ignore the fields if present (R-13.4-e). Mirrors TS `CACHEABLE_METHODS`. |
| static [CacheScopes](Caching/CacheScopes.md) { get; } | The two recognized `cacheScope` wire strings (§13.3). |
| static [MethodToNotification](Caching/MethodToNotification.md) { get; } | Maps each cacheable method to the notification that signals a change to its data; when the notification arrives the client MUST discard the cached result and re-fetch (§13.5, R-13.5-a/j). Mirrors TS `METHOD_TO_NOTIFICATION_MAP`. |
| static [EffectiveCacheScope](Caching/EffectiveCacheScope.md)(…) | Given the `cacheScope` values observed across a multi-page list, returns the effective scope to apply: the common scope when consistent, otherwise Private. An empty list resolves to private (the safe default). Mirrors TS `effectiveCacheScope`. (R-13.5-h) |
| static [ExpiresAt](Caching/ExpiresAt.md)(…) | Computes `expiresAt` — the absolute client-local timestamp after which the result is stale (`receivedAt + ttlMs`). Mirrors TS `expiresAt`. (R-13.2-e/f) |
| static [HasBothOrNeitherCacheHints](Caching/HasBothOrNeitherCacheHints.md)(…) | Returns `true` when a result object carries BOTH caching-hint fields, or NEITHER. A server MUST NOT emit exactly one without the other. Mirrors TS `hasBothOrNeitherCacheHints`. (R-13.1-g) |
| static [HasConsistentCacheScope](Caching/HasConsistentCacheScope.md)(…) | Returns `true` when all `cacheScope` values across the pages of one logical list are identical (no mixing of public and private). An empty or single-page list is consistent. Mirrors TS `hasConsistentCacheScope`. (R-13.5-f/g) |
| static [IsCacheableMethod](Caching/IsCacheableMethod.md)(…) | Returns `true` when *method* is one of the five cacheable methods. Mirrors TS `isCacheableMethod`. |
| static [IsCacheHintValid](Caching/IsCacheHintValid.md)(…) | Returns `true` when BOTH caching-hint fields are present and valid: *ttlMs* is a non-negative integer and *cacheScope* is exactly `"public"` or `"private"`. A receiver MUST NOT treat a result as cacheable when `ttlMs` is negative, non-integer, or missing, and MUST treat an unrecognized/missing scope as private (handled by [`ResolveCacheScope`](./Caching/ResolveCacheScope.md)). Mirrors TS `isCacheHintValid`. (R-13.1-a/b/d/e) |
| static [IsFresh](Caching/IsFresh.md)(…) | Returns `true` when the result is still within its freshness window: `(ttlMs > 0) AND (now < receivedAt + ttlMs)`. A `ttlMs` of `0` is never fresh (immediately stale). The computation uses ONLY the client's local *receivedAt* and the *ttlMs* value — it MUST NOT assume the client and server clocks agree. Mirrors TS `isFresh`. (R-13.2-e/f/g) |
| static [MethodsForNotification](Caching/MethodsForNotification.md)(…) | Returns the method names whose cached results should be invalidated when *notification* is received. Mirrors TS `methodsForNotification`. (R-13.5-j) |
| static [ResolveCacheScope](Caching/ResolveCacheScope.md)(…) | Returns Public or Private, applying the PRIVACY FALLBACK for any unrecognized or absent value — a receiver that cannot reliably distinguish authorization contexts MUST treat every cached result as private. Mirrors TS `resolveCacheScope`. (R-13.1-e, R-13.3-h) (2 methods) |
| static [ValidateCacheableComplete](Caching/ValidateCacheableComplete.md)(…) | Asserts the emit-side invariant for a cacheable result that is being sent with a completed (`"complete"`) discriminator (§3.6, §13.1, §13.4): the *resultType* MUST be exactly `"complete"`, and BOTH caching hints MUST be present — *ttlMs* a non-negative integer and *cacheScope* a resolved scope. A sender MUST NOT emit one hint without the other (§13.1), MUST NOT emit a negative `ttlMs` (§13.2), and MUST carry the discriminator (§3.6). This is the construction-time guard a server applies before a result reaches the wire; it is the counterpart of the lenient receive-side degradation in [`IsCacheHintValid`](./Caching/IsCacheHintValid.md)/[`ResolveCacheScope`](./Caching/ResolveCacheScope.md), which never throws. |

## Remarks

Caching hints are purely advisory: they never alter a result's meaning, never function as access control, and clients MAY ignore them entirely (R-13.4-f, R-13.3-d/e).

## See Also

* namespace [Stackific.Mcp.Protocol](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
