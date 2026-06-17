[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / classifyErrorCode

# Function: classifyErrorCode()

> **classifyErrorCode**(`code`): [`ErrorCodeClass`](../type-aliases/ErrorCodeClass.md)

Defined in: [protocol/errors.ts:262](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/errors.ts#L262)

Classifies any integer `code` into one of the [ErrorCodeClass](../variables/ErrorCodeClass.md) ranges,
even codes not present in the registry. A registry entry's own `class` always
wins; otherwise the code is placed by range: the server-error sub-range
(`-32000..-32099`) → `SERVER_DEFINED`, any other reserved-range code →
`JSON_RPC_STANDARD`, and everything outside the reserved range →
`EXTENSION_DEFINED`. (§22.2, §22.7, R-22.7-a)

## Parameters

### code

`number`

## Returns

[`ErrorCodeClass`](../type-aliases/ErrorCodeClass.md)
