[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ConsentRequest

# Interface: ConsentRequest

Defined in: [protocol/security.ts:326](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L326)

A proposed operation seeking the host's consent gate. (§28.2)

## Properties

### operation

> **operation**: `string`

Defined in: [protocol/security.ts:328](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L328)

The operation being proposed.

***

### scope

> **scope**: `string`

Defined in: [protocol/security.ts:330](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L330)

The scope summary of the proposed operation, compared against any prior grant.

***

### userApproved?

> `optional` **userApproved?**: `boolean`

Defined in: [protocol/security.ts:337](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L337)

Whether the user has, for THIS proposal, actively and informedly granted
consent. Silence/absence MUST NOT be passed as `true` (R-28.2-d). When the
proposal matches a prior grant of the same operation+scope, a fresh active
grant is not required.
