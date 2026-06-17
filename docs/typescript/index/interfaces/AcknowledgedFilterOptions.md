[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / AcknowledgedFilterOptions

# Interface: AcknowledgedFilterOptions

Defined in: [protocol/streaming.ts:357](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L357)

Options refining the honored-subset computation beyond the core §10 server
capabilities — currently just the Tasks extension's per-request activation.

## Properties

### tasksActive?

> `optional` **tasksActive?**: `boolean`

Defined in: [protocol/streaming.ts:367](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L367)

Whether the Tasks extension (`io.modelcontextprotocol/tasks`) is active for
THIS `subscriptions/listen` request — i.e. the client declared it in this
request's `clientCapabilities.extensions` AND the server advertises it under
`capabilities.extensions`. A `taskIds` opt-in is honored ONLY when this is
`true`; otherwise it is dropped (and the transport rejects the request with
`-32003` before this is reached). Defaults to `false`. (§25.2, §25.10,
R-25.10-e, R-25.10-f)
