[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RequestDisplayModeResultSchema

# Variable: RequestDisplayModeResultSchema

> `const` **RequestDisplayModeResultSchema**: `ZodObject`\<\{ `mode`: `ZodEnum`\<\[`"inline"`, `"fullscreen"`, `"pip"`\]\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `mode`: `ZodEnum`\<\[`"inline"`, `"fullscreen"`, `"pip"`\]\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `mode`: `ZodEnum`\<\[`"inline"`, `"fullscreen"`, `"pip"`\]\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [protocol/ui-host.ts:588](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui-host.ts#L588)

`RequestDisplayModeResult` — result of `ui/request-display-mode`: the mode the
host ACTUALLY applied, which MAY differ from the requested mode. (§26.5.3,
R-26.5.3-e; AC-42.9)
