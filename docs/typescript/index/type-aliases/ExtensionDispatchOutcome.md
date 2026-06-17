[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ExtensionDispatchOutcome

# Type Alias: ExtensionDispatchOutcome\<Result\>

> **ExtensionDispatchOutcome**\<`Result`\> = \{ `ok`: `true`; `result`: `Result`; \} \| \{ `ok`: `false`; `reason`: [`ExtensionDispatchRejection`](ExtensionDispatchRejection.md); `code`: *typeof* [`INVALID_PARAMS_CODE`](../variables/INVALID_PARAMS_CODE.md); \}

Defined in: [protocol/extension-mechanism.ts:741](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/extension-mechanism.ts#L741)

Outcome of [ExtensionMethodRouter.dispatch](../classes/ExtensionMethodRouter.md#dispatch).

## Type Parameters

### Result

`Result` = `unknown`
