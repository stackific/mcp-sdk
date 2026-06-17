[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ConsentDecision

# Type Alias: ConsentDecision

> **ConsentDecision** = \{ `allowed`: `true`; `reason`: `"matches-prior-grant"` \| `"freshly-approved"`; \} \| \{ `allowed`: `false`; `reason`: `"no-consent"` \| `"not-informed"` \| `"material-change"` \| `"silent-escalation"`; `detail`: `string`; \}

Defined in: [protocol/security.ts:341](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L341)

The §28.2 consent-gate decision.
