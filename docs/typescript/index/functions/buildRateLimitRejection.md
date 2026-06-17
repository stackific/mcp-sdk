[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / buildRateLimitRejection

# Function: buildRateLimitRejection()

> **buildRateLimitRejection**(`retryAfterMs?`, `message?`): [`RateLimitRejectionError`](../interfaces/RateLimitRejectionError.md)

Defined in: [protocol/security.ts:603](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L603)

Builds the `-32600` rate-limit rejection error a server returns for a `tools/call`
that exceeds the limit, matching the §28.3 wire example. (§28.3, R-28.3-h;
AC-44.9)

## Parameters

### retryAfterMs?

`number`

OPTIONAL hint for when the client may retry.

### message?

`string`

OPTIONAL override for the error message.

## Returns

[`RateLimitRejectionError`](../interfaces/RateLimitRejectionError.md)
