[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildHeaderMismatch

# Function: buildHeaderMismatch()

> **buildHeaderMismatch**(`message?`): [`HttpRejection`](../interfaces/HttpRejection.md)

Defined in: [transport/http/headers.ts:98](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/transport/http/headers.ts#L98)

Builds a `HeaderMismatch` (`-32001`) rejection (HTTP `400`). (§9.3–§9.4)

## Parameters

### message?

`string` = `'Header does not match request body'`

## Returns

[`HttpRejection`](../interfaces/HttpRejection.md)
