[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / headerMismatchForCause

# Function: headerMismatchForCause()

> **headerMismatchForCause**(`cause`): `objectOutputType`

Defined in: [transport/http/responses.ts:363](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/transport/http/responses.ts#L363)

Builds a `-32001` `HeaderMismatch` error object from a structured cause,
producing a descriptive message for each of the conditions §9.8 enumerates.
(R-9.8-b, R-9.8-c, R-9.8-d)

## Parameters

### cause

[`HeaderMismatchCause`](../type-aliases/HeaderMismatchCause.md)

## Returns

`objectOutputType`
