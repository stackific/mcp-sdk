[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ExtensionClassification

# Type Alias: ExtensionClassification

> **ExtensionClassification** = `"modular"` \| `"specialized"` \| `"experimental"`

Defined in: [protocol/extension-mechanism.ts:189](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extension-mechanism.ts#L189)

The three (non-exclusive) ways an extension may be characterized. (§24.1,
R-24.1-a) An extension is classifiable as one of these; the value is purely
descriptive and does not affect negotiation.

  - `modular`      — a discrete capability;
  - `specialized`  — domain- or industry-specific behavior;
  - `experimental` — incubated for possible future inclusion in the core.
