[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayPlaceInputRequest

# Function: mayPlaceInputRequest()

> **mayPlaceInputRequest**(`method`, `clientCapabilities`): `boolean`

Defined in: [protocol/conformance-requirements.ts:645](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/conformance-requirements.ts#L645)

Returns `true` when a server MAY place an input request of `method` into an
`input_required` result for a client declaring `clientCapabilities`. (§29.4
item 5, R-29.4-l) An unrecognized method is rejected (`false`): a server must
not solicit a kind it cannot tie to a declared capability.

Delegates to S17's [mayEmitInputRequestKind](mayEmitInputRequestKind.md) — the SAME gate the live
server now enforces in its solicitation path (`McpServer`'s `InputCollector`,
rejecting an undeclared kind with `-32003`). Sharing one implementation keeps
the conformance model and the runtime from drifting: this is no longer an
unwired shadow-spec, it is the runtime's own gate expressed at the model layer.

## Parameters

### method

`string`

### clientCapabilities

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
