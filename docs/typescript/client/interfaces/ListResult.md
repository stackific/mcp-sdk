[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / ListResult

# Interface: ListResult

Defined in: [client/client.ts:134](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L134)

A paginated list result: a method-specific item array plus an optional `nextCursor`. (§12.2)

## Indexable

> \[`key`: `string`\]: `unknown`

The page array lives under a method-specific key (`tools`, `resources`, `prompts`, …).

## Properties

### nextCursor?

> `optional` **nextCursor?**: `string`

Defined in: [client/client.ts:138](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L138)

Opaque cursor for the next page; absent at the end.
