[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / recordConsentGrant

# Function: recordConsentGrant()

> **recordConsentGrant**(`request`): [`ConsentGrant`](../interfaces/ConsentGrant.md)

Defined in: [protocol/security.ts:431](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L431)

Builds the [ConsentGrant](../interfaces/ConsentGrant.md) to persist after a successful, informed approval,
so a later identical operation matches without re-prompting. (R-28.2-b, R-28.2-f)

Only call after the user has actively and informedly approved; the resulting
grant records the operation+scope that [evaluateConsent](evaluateConsent.md) compares against.

## Parameters

### request

[`ConsentRequest`](../interfaces/ConsentRequest.md) & `object`

The freshly-approved operation.

## Returns

[`ConsentGrant`](../interfaces/ConsentGrant.md)
