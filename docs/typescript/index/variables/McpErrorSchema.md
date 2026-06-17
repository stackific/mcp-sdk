[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / McpErrorSchema

# Variable: McpErrorSchema

> `const` **McpErrorSchema**: `ZodObject`\<\{ `code`: `ZodEffects`\<`ZodNumber`, `number`, `number`\>; `message`: `ZodString`; `data`: `ZodOptional`\<`ZodUnknown`\>; \}, `"passthrough"`, `ZodTypeAny`, `objectOutputType`\<\{ `code`: `ZodEffects`\<`ZodNumber`, `number`, `number`\>; `message`: `ZodString`; `data`: `ZodOptional`\<`ZodUnknown`\>; \}, `ZodTypeAny`, `"passthrough"`\>, `objectInputType`\<\{ `code`: `ZodEffects`\<`ZodNumber`, `number`, `number`\>; `message`: `ZodString`; `data`: `ZodOptional`\<`ZodUnknown`\>; \}, `ZodTypeAny`, `"passthrough"`\>\>

Defined in: [jsonrpc/payload.ts:222](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/jsonrpc/payload.ts#L222)

The object carried in the `error` member of every error response. (§3.8)

Named `McpError` to avoid shadowing the built-in `Error` class.

Fields:
  `code` (REQUIRED integer): identifies the error condition. Legal values and
  their use conditions are defined in §22 / S34. Implementations MUST NOT
  assign codes outside those rules. (R-3.8-a, R-3.8-b)

  `message` (REQUIRED string): short, human-readable description. SHOULD be
  a single concise sentence. (R-3.8-c, R-3.8-d)

  `data` (OPTIONAL any): sender-defined additional info. Receivers MUST NOT
  assume a particular structure unless the specific code defines one in §22.
  (R-3.8-e, R-3.8-f)
