[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ELICIT\_ACTION

# Variable: ELICIT\_ACTION

> `const` **ELICIT\_ACTION**: `object`

Defined in: [protocol/elicitation-form.ts:580](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L580)

The three `ElicitResult.action` literals — the user's intent, applicable to
both form and URL modes. (§20.5, R-20.5-a)

  `"accept"`  — the user approved and submitted (form: `content` carries the
                data; url: acceptance signals consent, NOT completion).
  `"decline"` — the user explicitly refused.
  `"cancel"`  — the user dismissed without choosing.

## Type Declaration

### ACCEPT

> `readonly` **ACCEPT**: `"accept"` = `'accept'`

### DECLINE

> `readonly` **DECLINE**: `"decline"` = `'decline'`

### CANCEL

> `readonly` **CANCEL**: `"cancel"` = `'cancel'`
