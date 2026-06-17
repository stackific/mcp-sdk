[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ServerUiAcknowledgementSchema

# Variable: ServerUiAcknowledgementSchema

> `const` **ServerUiAcknowledgementSchema**: `ZodObject`\<\{ \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/ui.ts:380](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L380)

`ServerUiAcknowledgement` — the value a server places under
[UI\_EXTENSION\_ID](UI_EXTENSION_ID.md) in `capabilities.extensions` of its `server/discover`
result to acknowledge the extension. It is an object that MAY be empty (`{}`);
presence of the key is what signals acknowledgement. (§26.2, R-26.2-j)

`.passthrough()` allows forward-compatible members an extension version may
add to the acknowledgement object.
