[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isTokenResponse

# Function: isTokenResponse()

> **isTokenResponse**(`value`): `value is objectOutputType<{ access_token: ZodString; token_type: ZodString; expires_in: ZodOptional<ZodNumber>; refresh_token: ZodOptional<ZodString>; scope: ZodOptional<ZodString> }, ZodTypeAny, "passthrough">`

Defined in: [protocol/authorization-flow.ts:1367](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L1367)

Returns `true` when `value` is a structurally valid token response.

## Parameters

### value

`unknown`

## Returns

`value is objectOutputType<{ access_token: ZodString; token_type: ZodString; expires_in: ZodOptional<ZodNumber>; refresh_token: ZodOptional<ZodString>; scope: ZodOptional<ZodString> }, ZodTypeAny, "passthrough">`
