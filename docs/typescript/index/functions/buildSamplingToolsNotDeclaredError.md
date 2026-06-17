[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildSamplingToolsNotDeclaredError

# Function: buildSamplingToolsNotDeclaredError()

> **buildSamplingToolsNotDeclaredError**(`field`): `object`

Defined in: [protocol/sampling.ts:542](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L542)

The JSON-RPC error a client MUST return when a sampling input request includes
`tools` or `toolChoice` but the client did not declare `sampling.tools`.
(R-21.2.3-b, R-21.2.4-n, R-21.2.4-o)

Code is `-32602` (Invalid params, §22). `field` names the offending member.

## Parameters

### field

`"tools"` \| `"toolChoice"`

## Returns

`object`

### code

> **code**: `-32602`

### message

> **message**: `string`
