[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildResourceReadInternalError

# Function: buildResourceReadInternalError()

> **buildResourceReadInternalError**(`message?`): `object`

Defined in: [protocol/resources-read.ts:162](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L162)

Builds the `-32603` (Internal error) a server SHOULD return for a failure
UNRELATED to the validity of the requested `uri` (e.g. a backing store is
unreachable). Distinct from [buildResourceNotFoundError](buildResourceNotFoundError.md), which is for a
`uri` that simply does not exist. (§17.6, R-17.6-d)

## Parameters

### message?

`string` = `'Internal error reading resource'`

## Returns

`object`

### code

> **code**: `-32603`

### message

> **message**: `string`
