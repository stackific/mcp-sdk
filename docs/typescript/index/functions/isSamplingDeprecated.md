[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isSamplingDeprecated

# Function: isSamplingDeprecated()

> **isSamplingDeprecated**(): `boolean`

Defined in: [protocol/sampling.ts:76](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L76)

Returns `true` when sampling is Deprecated — it always is. Provided so callers
(and conformance reviewers) can branch on the deprecation posture without
hard-coding the constant. Mirrors `isDeprecatedClientCapability('sampling')`.
(R-21.2-a, R-21.2.1-a)

## Returns

`boolean`
