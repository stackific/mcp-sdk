[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ElicitationCompleteHandling

# Type Alias: ElicitationCompleteHandling

> **ElicitationCompleteHandling** = \{ `action`: `"ignore"`; `reason`: `"unknown-id"` \| `"already-completed"`; \} \| \{ `action`: `"complete"`; `elicitationId`: `string`; \}

Defined in: [protocol/elicitation-form.ts:1086](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1086)

Outcome of [handleElicitationComplete](../functions/handleElicitationComplete.md): what a client should do with a
received completion notification given the state it has tracked.

## Union Members

### Type Literal

\{ `action`: `"ignore"`; `reason`: `"unknown-id"` \| `"already-completed"`; \}

Unknown or already-completed id ⇒ MUST ignore, take no action. (R-20.6-d)

***

### Type Literal

\{ `action`: `"complete"`; `elicitationId`: `string`; \}

A pending id just completed ⇒ MAY auto-retry / update UI / continue. (R-20.6-e)
