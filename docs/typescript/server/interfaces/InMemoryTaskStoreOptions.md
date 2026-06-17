[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / InMemoryTaskStoreOptions

# Interface: InMemoryTaskStoreOptions

Defined in: [server/tasks.ts:39](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L39)

Options for [InMemoryTaskStore](../classes/InMemoryTaskStore.md).

## Properties

### now?

> `optional` **now?**: () => `number`

Defined in: [server/tasks.ts:41](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L41)

Clock injection (default `Date.now`); lets tests drive ttl expiry deterministically.

#### Returns

`number`

***

### defaultPollIntervalMs?

> `optional` **defaultPollIntervalMs?**: `number`

Defined in: [server/tasks.ts:43](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L43)

Optional `pollIntervalMs` hint stamped on every created task. (§25.4)
