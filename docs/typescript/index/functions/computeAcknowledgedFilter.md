[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / computeAcknowledgedFilter

# Function: computeAcknowledgedFilter()

> **computeAcknowledgedFilter**(`requested`, `serverCaps`, `options?`): `objectOutputType`

Defined in: [protocol/streaming.ts:386](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L386)

Computes the honored-subset `SubscriptionFilter` for the acknowledgement: a kind
is honored only when the client requested it AND the gating server
capability/sub-flag is declared. Unsupported kinds are OMITTED entirely. (§10.3,
R-10.3-c, R-10.3-d; gating per R-10.5-l)

For `resourceSubscriptions`, the honored list is the requested URIs (subset the
server agrees to watch) when `resources.subscribe` is declared, else omitted.

For `taskIds`, the honored list is the requested ids when the Tasks extension is
active for the request (`options.tasksActive`), else omitted. (§25.10)

## Parameters

### requested

`objectOutputType`

The client's requested filter.

### serverCaps

`Record`\<`string`, `unknown`\>

The server's declared `ServerCapabilities`.

### options?

[`AcknowledgedFilterOptions`](../interfaces/AcknowledgedFilterOptions.md) = `{}`

Extension-activation refinements (e.g. `tasksActive`).

## Returns

`objectOutputType`
