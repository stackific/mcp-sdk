[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / securityRequirementsForPrinciple

# Function: securityRequirementsForPrinciple()

> **securityRequirementsForPrinciple**(`principle`): [`SecurityRequirement`](../interfaces/SecurityRequirement.md)[]

Defined in: [protocol/security.ts:247](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L247)

Returns every §28 requirement that derives from a given core principle, in spec
order — the per-principle slice of the baseline. (R-28.1-a)

## Parameters

### principle

`"user-consent-and-control"` \| `"data-privacy"` \| `"tool-safety"` \| `"host-mediated-trust"`

One of the four core principles.

## Returns

[`SecurityRequirement`](../interfaces/SecurityRequirement.md)[]
