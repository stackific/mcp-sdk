[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildReadResourceRetryParams

# Function: buildReadResourceRetryParams()

> **buildReadResourceRetryParams**(`uri`, `inputRequests`, `inputResponses`, `requestState?`): `objectOutputType`

Defined in: [protocol/resources-read.ts:270](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L270)

Builds the retry params for a `resources/read` that the server answered with
`input_required`. Every key in the server's `inputRequests` MUST be answered
in `inputResponses`; the prior `requestState` (when the server supplied one)
is echoed back BYTE-FOR-BYTE unchanged. (§17.5, R-17.5-e, R-17.5-g, R-17.5-h, R-17.5-x)

## Parameters

### uri

`string`

The same resource URI as the original request.

### inputRequests

`Record`\<`string`, `unknown`\>

The server's earlier `inputRequests` (its key set).

### inputResponses

`Record`\<`string`, `unknown`\>

The client's responses; MUST cover every `inputRequests` key.

### requestState?

`string`

The opaque token from the `input_required` result, if any.

## Returns

`objectOutputType`

## Throws

When `inputResponses` does not answer every `inputRequests` key. (R-17.5-e)
