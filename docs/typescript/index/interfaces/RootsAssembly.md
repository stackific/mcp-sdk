[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RootsAssembly

# Interface: RootsAssembly

Defined in: [protocol/roots.ts:459](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L459)

Outcome of [assembleListRootsResult](../functions/assembleListRootsResult.md).

## Properties

### result

> **result**: `objectOutputType`

Defined in: [protocol/roots.ts:461](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L461)

The validated listing to supply as the `roots/list` input response.

***

### excluded

> **excluded**: `object`[]

Defined in: [protocol/roots.ts:463](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/roots.ts#L463)

Candidates excluded, with the reason each was dropped.

#### root

> **root**: `objectOutputType`

#### reason

> **reason**: `"not-in-scope"` \| `"no-consent"` \| `"invalid-uri"` \| `"path-traversal"`
