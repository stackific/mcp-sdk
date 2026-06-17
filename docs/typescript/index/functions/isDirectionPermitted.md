[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isDirectionPermitted

# Function: isDirectionPermitted()

> **isDirectionPermitted**(`kind`, `direction`): `boolean`

Defined in: [transport/contract.ts:141](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/transport/contract.ts#L141)

Returns `true` when a message of `kind` may travel in `direction`. (§7.4)

Permitted directions (R-7.4-b, R-7.4-c, and the informative rule that servers
never initiate requests and clients never send responses):
  - `request`      → client→server only
  - `response`     → server→client only
  - `notification` → either direction

## Parameters

### kind

[`DirectionalKind`](../type-aliases/DirectionalKind.md)

### direction

[`MessageDirection`](../type-aliases/MessageDirection.md)

## Returns

`boolean`
