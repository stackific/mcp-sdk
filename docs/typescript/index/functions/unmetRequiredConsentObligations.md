[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / unmetRequiredConsentObligations

# Function: unmetRequiredConsentObligations()

> **unmetRequiredConsentObligations**(`obligations`): keyof [`SamplingConsentObligations`](../interfaces/SamplingConsentObligations.md)[]

Defined in: [protocol/sampling.ts:880](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L880)

Verifies that the MUST-level §21.2.10 obligations are met. Returns the list of
unmet MUST obligations; an empty list means the hard requirements are
satisfied. SHOULD-level obligations are advisory and not failed here.
(R-21.2.10-a, R-21.2.10-b, R-21.2.10-h)

## Parameters

### obligations

[`SamplingConsentObligations`](../interfaces/SamplingConsentObligations.md)

## Returns

keyof [`SamplingConsentObligations`](../interfaces/SamplingConsentObligations.md)[]
