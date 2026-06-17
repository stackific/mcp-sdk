[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayDisplayToolAnnotations

# Function: mayDisplayToolAnnotations()

> **mayDisplayToolAnnotations**(`serverIsTrusted`): `boolean`

Defined in: [protocol/security.ts:484](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L484)

Returns whether a host MAY surface a tool's annotation hints to the user for
THIS server — delegating to S25's [mayTrustToolAnnotations](mayTrustToolAnnotations.md). Displaying a
hint from a trusted server is permitted (R-28.3-b); relying on it as a guarantee
is not ([toolAnnotationIsSecurityGuarantee](toolAnnotationIsSecurityGuarantee.md), R-28.3-c). (§28.3; AC-44.6)

## Parameters

### serverIsTrusted

`boolean`

Whether the host explicitly trusts the server.

## Returns

`boolean`
