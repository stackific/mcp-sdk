[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isValidElicitationUrl

# Function: isValidElicitationUrl()

> **isValidElicitationUrl**(`url`): `boolean`

Defined in: [protocol/elicitation.ts:381](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation.ts#L381)

Returns `true` when `url` is a valid, absolute URI/URL per RFC 3986 — the
requirement on the url-mode `url` field. (§20.3, R-20.3-m, R-20.3-n)

Uses the WHATWG `URL` parser (absolute URLs only); relative references and
malformed strings are rejected.

## Parameters

### url

`unknown`

## Returns

`boolean`
