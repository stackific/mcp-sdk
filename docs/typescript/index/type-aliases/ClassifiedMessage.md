[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ClassifiedMessage

# Type Alias: ClassifiedMessage

> **ClassifiedMessage** = \{ `kind`: `"request"`; `message`: [`JSONRPCRequest`](JSONRPCRequest.md); \} \| \{ `kind`: `"notification"`; `message`: [`JSONRPCNotification`](JSONRPCNotification.md); \} \| \{ `kind`: `"result-response"`; `message`: [`JSONRPCResultResponse`](JSONRPCResultResponse.md); \} \| \{ `kind`: `"error-response"`; `message`: [`JSONRPCErrorResponse`](JSONRPCErrorResponse.md); \}

Defined in: [jsonrpc/framing.ts:150](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/jsonrpc/framing.ts#L150)

Returned by `classifyMessage` when the message is valid.
