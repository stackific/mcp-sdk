[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildMalformedRetryError

# Function: buildMalformedRetryError()

> **buildMalformedRetryError**(`detail`): `object`

Defined in: [protocol/multi-round-trip.ts:509](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L509)

Builds the JSON-RPC error payload for a protocol-malformed retry request.
(R-11.5-s)

A server MUST return a JSON-RPC error (not another `InputRequiredResult`)
when the retry's `inputResponses` is malformed at the protocol level —
for example, a response value that does not match the declared `InputResponse`
shape for its key.

Error code is `-32602` (Invalid params, §22).

## Parameters

### detail

`string`

## Returns

`object`

### code

> **code**: `-32602`

### message

> **message**: `string`
