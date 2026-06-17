[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ElicitationUserDecision

# Type Alias: ElicitationUserDecision

> **ElicitationUserDecision** = `"approve"` \| `"edit"` \| `"decline"` \| `"cancel"`

Defined in: [protocol/security.ts:1050](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L1050)

The terminal user decision on a server-initiated elicitation. (§28.7,
R-28.7-b, R-28.7-c) Mirrors S31's `ElicitAction` outcomes; a user MUST be able
to reach `decline`/`cancel` at any point.
