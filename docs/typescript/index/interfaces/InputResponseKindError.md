[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / InputResponseKindError

# Interface: InputResponseKindError

Defined in: [protocol/multi-round-trip.ts:417](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L417)

One kind-correlation failure reported by `validateInputResponseKinds`.

## Properties

### key

> **key**: `string`

Defined in: [protocol/multi-round-trip.ts:419](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L419)

The `inputResponses` key whose value failed validation.

***

### expectedMethod

> **expectedMethod**: `string`

Defined in: [protocol/multi-round-trip.ts:421](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L421)

The `InputRequest.method` the server sent under this key.

***

### detail

> **detail**: `string`

Defined in: [protocol/multi-round-trip.ts:423](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/multi-round-trip.ts#L423)

Human-readable Zod error detail.
