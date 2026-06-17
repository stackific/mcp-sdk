[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / mayCallPromptMethod

# Function: mayCallPromptMethod()

> **mayCallPromptMethod**(`method`, `serverCaps`): `boolean`

Defined in: [protocol/prompts.ts:148](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/prompts.ts#L148)

Returns `true` when a client MAY send `method` (`prompts/list` or `prompts/get`)
given the server's declared capabilities. A client MUST NOT send either method
to a server that has not declared `prompts`. (R-18.1-b, AC-28.3)

Delegates to `mayClientInvoke` (S10), whose method→capability map already gates
both prompt methods on the `prompts` capability.

## Parameters

### method

`string`

### serverCaps

`Record`\<`string`, `unknown`\>

## Returns

`boolean`
