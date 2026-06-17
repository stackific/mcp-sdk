[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SamplingGateResult

# Type Alias: SamplingGateResult

> **SamplingGateResult** = \{ `ok`: `true`; \} \| \{ `ok`: `false`; `error`: `ReturnType`\<*typeof* [`buildSamplingToolsNotDeclaredError`](../functions/buildSamplingToolsNotDeclaredError.md)\>; \}

Defined in: [protocol/sampling.ts:553](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L553)

Outcome of [gateSamplingToolUse](../functions/gateSamplingToolUse.md) / [validateSamplingRequest](../functions/validateSamplingRequest.md).
