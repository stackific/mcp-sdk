[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildServerUiAcknowledgement

# Function: buildServerUiAcknowledgement()

> **buildServerUiAcknowledgement**(): `object`

Defined in: [protocol/ui.ts:392](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L392)

Builds the `capabilities.extensions` fragment a server includes in its
`server/discover` result to acknowledge the apps extension: a single
[UI\_EXTENSION\_ID](../variables/UI_EXTENSION_ID.md) key mapped to an empty object. (§26.2, R-26.2-j)

Acknowledgement is OPTIONAL (MAY); a server merges this fragment into the
`extensions` map of its result capabilities when it chooses to acknowledge.

## Returns

`object`

#### io.modelcontextprotocol/ui

> **io.modelcontextprotocol/ui**: `objectOutputType`
