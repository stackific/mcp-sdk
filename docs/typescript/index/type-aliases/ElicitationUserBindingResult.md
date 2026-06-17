[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ElicitationUserBindingResult

# Type Alias: ElicitationUserBindingResult

> **ElicitationUserBindingResult** = \{ `ok`: `true`; \} \| \{ `ok`: `false`; `reason`: `"subject-mismatch"`; `expected`: `string`; `actual`: `string`; \} \| \{ `ok`: `false`; `reason`: `"unverified-identity"`; `detail`: `string`; \}

Defined in: [protocol/elicitation-form.ts:1383](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1383)

Outcome of [verifyElicitationUserBinding](../functions/verifyElicitationUserBinding.md).

## Union Members

### Type Literal

\{ `ok`: `true`; \}

***

### Type Literal

\{ `ok`: `false`; `reason`: `"subject-mismatch"`; `expected`: `string`; `actual`: `string`; \}

The two sessions resolve to different subjects ⇒ reject. (R-20.7-m)

***

### Type Literal

\{ `ok`: `false`; `reason`: `"unverified-identity"`; `detail`: `string`; \}

A subject was missing or client-provided-only ⇒ cannot verify. (R-20.7-j, R-20.7-k)
