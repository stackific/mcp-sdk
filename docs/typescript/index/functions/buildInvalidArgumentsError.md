[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildInvalidArgumentsError

# Function: buildInvalidArgumentsError()

> **buildInvalidArgumentsError**(`name`, `errors?`): [`ToolProtocolError`](../interfaces/ToolProtocolError.md)

Defined in: [protocol/tools-call.ts:416](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tools-call.ts#L416)

Builds the JSON-RPC error for an ARGUMENT-VALIDATION failure — `arguments` that
do not conform to the tool's `inputSchema`. MUST be reported with code `-32602`
(Invalid params), as a JSON-RPC error and never as a `CallToolResult`; the tool
MUST NOT be invoked. (§16.6, R-16.5-d, R-16.6-d, R-16.6-f)

## Parameters

### name

`string`

The tool name whose arguments failed validation.

### errors?

readonly `string`[] = `[]`

OPTIONAL validation error detail (e.g. from `validateToolArguments`).

## Returns

[`ToolProtocolError`](../interfaces/ToolProtocolError.md)
