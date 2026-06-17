[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CredentialBindingDecision

# Interface: CredentialBindingDecision

Defined in: [protocol/authorization-registration.ts:489](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-registration.ts#L489)

Outcome of [decideCredentialBinding](../functions/decideCredentialBinding.md).

## Properties

### action

> **action**: [`CredentialBindingAction`](../type-aliases/CredentialBindingAction.md)

Defined in: [protocol/authorization-registration.ts:491](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-registration.ts#L491)

Whether to reuse the stored credentials, re-register, or surface an error.

***

### reason

> **reason**: `string`

Defined in: [protocol/authorization-registration.ts:493](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-registration.ts#L493)

Human-readable explanation, suitable for surfacing to a user/developer.
