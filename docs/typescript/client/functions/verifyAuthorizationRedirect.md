[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / verifyAuthorizationRedirect

# Function: verifyAuthorizationRedirect()

> **verifyAuthorizationRedirect**(`options`): `void`

Defined in: [client/oauth.ts:122](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/oauth.ts#L122)

Verifies the authorization redirect before redeeming the code (§23.5 / §23.7):
the returned `state` MUST equal the value sent in step 1 (CSRF defense), and the
returned `iss` MUST be validated against the recorded issuer per the §23.7
decision table. Crucially, a PRESENT `iss` is compared by EXACT string match
EVEN WHEN the AS does not advertise `authorization_response_iss_parameter_supported`
(R-23.7-f) — the mix-up defense the wired client previously skipped when the flag
was false/absent. Throws on any mismatch. Edge-safe (pure string comparison).

Delegates to the reference `verifyRedirectState` / `validateIssuer` so the wired
OAuth client and the protocol model enforce identical rules.

## Parameters

### options

#### sentState

`string`

#### returnedState?

`string` \| `null`

#### issuer

`string`

#### returnedIss?

`string` \| `null`

#### issParameterSupported?

`boolean`

## Returns

`void`
