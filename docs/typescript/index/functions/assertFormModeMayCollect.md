[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / assertFormModeMayCollect

# Function: assertFormModeMayCollect()

> **assertFormModeMayCollect**(`requestedSchema`): [`SensitiveFieldCheck`](../type-aliases/SensitiveFieldCheck.md)

Defined in: [protocol/elicitation-form.ts:1204](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1204)

Asserts that a form-mode `requestedSchema` does not request sensitive
credential data — the §20.7 prohibition. (§20.7, R-20.7-h, R-20.7-i)

Returns `{ ok: true }` when no sensitive fields are detected, or
`{ ok: false, sensitiveFields }` naming the offending fields; the server MUST
then use URL mode for those interactions instead. (R-20.7-i)

## Parameters

### requestedSchema

`unknown`

## Returns

[`SensitiveFieldCheck`](../type-aliases/SensitiveFieldCheck.md)
