[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / withinToolLoopLimit

# Function: withinToolLoopLimit()

> **withinToolLoopLimit**(`iteration`, `limit`): `boolean`

Defined in: [protocol/sampling.ts:895](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L895)

Enforces a tool-loop iteration limit during sampling tool use; both parties
SHOULD apply such a limit. (R-21.2.10-i) Returns `true` when another iteration
is permitted (current count is below the limit), `false` when the limit is
reached and the loop MUST stop.

## Parameters

### iteration

`number`

The zero-based count of tool-loop iterations already run.

### limit

`number`

The maximum number of tool-loop iterations allowed.

## Returns

`boolean`
