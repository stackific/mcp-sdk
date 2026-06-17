[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SamplingConsentObligations

# Interface: SamplingConsentObligations

Defined in: [protocol/sampling.ts:843](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L843)

The consent & safety obligations a conforming client/host MUST or SHOULD honor
around sampling. (§21.2.10) Surfaced as a structured checklist so a host can
assert it satisfies each obligation and so conformance reviews can enumerate
them. The booleans report which obligations a host claims to meet.

## Properties

### humanInTheLoop

> **humanInTheLoop**: `boolean`

Defined in: [protocol/sampling.ts:845](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L845)

MUST keep a human in the loop. (R-21.2.10-a)

***

### userMayDeny

> **userMayDeny**: `boolean`

Defined in: [protocol/sampling.ts:847](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L847)

MUST let the user deny a sampling request. (R-21.2.10-b)

***

### reviewPromptBeforeSampling

> **reviewPromptBeforeSampling**: `boolean`

Defined in: [protocol/sampling.ts:849](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L849)

SHOULD present the prompt for review/edit/reject before sampling. (R-21.2.10-c)

***

### reviewResultBeforeServer

> **reviewResultBeforeServer**: `boolean`

Defined in: [protocol/sampling.ts:851](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L851)

SHOULD present the result for review/edit/reject before the server sees it. (R-21.2.10-d)

***

### mayModifyControlFields

> **mayModifyControlFields**: `boolean`

Defined in: [protocol/sampling.ts:853](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L853)

MAY modify/omit systemPrompt/includeContext/temperature/stopSequences/metadata. (R-21.2.10-e)

***

### rateLimiting

> **rateLimiting**: `boolean`

Defined in: [protocol/sampling.ts:855](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L855)

SHOULD implement rate limiting. (R-21.2.10-f)

***

### validateContent

> **validateContent**: `boolean`

Defined in: [protocol/sampling.ts:857](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L857)

SHOULD validate message content (both parties). (R-21.2.10-g)

***

### handleSensitiveData

> **handleSensitiveData**: `boolean`

Defined in: [protocol/sampling.ts:859](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L859)

MUST handle sensitive data appropriately (both parties). (R-21.2.10-h)

***

### toolLoopIterationLimits

> **toolLoopIterationLimits**: `boolean`

Defined in: [protocol/sampling.ts:861](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L861)

SHOULD implement iteration limits for tool loops when tools are used. (R-21.2.10-i)
