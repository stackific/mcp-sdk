[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ExtensionFallbackDecision

# Type Alias: ExtensionFallbackDecision

> **ExtensionFallbackDecision** = `"use-extension"` \| `"fallback"` \| `"reject"`

Defined in: [protocol/extensions.ts:323](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extensions.ts#L323)

What a peer should do for an operation that COULD use an extension which is
not active in the intersection.

  - `'use-extension'` — the extension is active; exercise its behavior.
  - `'fallback'`      — not active, but the operation has a core fallback.
  - `'reject'`        — not active and the extension is MANDATORY for this
                        operation; reject with an appropriate error.
