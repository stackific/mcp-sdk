[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / UrlConsentPresentation

# Interface: UrlConsentPresentation

Defined in: [protocol/elicitation-form.ts:1307](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1307)

What a client must surface to the user before consenting to open a URL. (§20.7)

## Properties

### fullUrl

> **fullUrl**: `string`

Defined in: [protocol/elicitation-form.ts:1309](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1309)

The full URL shown verbatim for examination. (R-20.7-v)

***

### host

> **host**: `string`

Defined in: [protocol/elicitation-form.ts:1311](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1311)

The host to highlight (mitigates subdomain spoofing). (R-20.7-v, R-20.7-x)

***

### domain

> **domain**: `string`

Defined in: [protocol/elicitation-form.ts:1313](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1313)

The registrable-ish domain portion highlighted to the user. (R-20.7-x)

***

### scheme

> **scheme**: `string`

Defined in: [protocol/elicitation-form.ts:1315](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1315)

The URL scheme.

***

### containsPunycode

> **containsPunycode**: `boolean`

Defined in: [protocol/elicitation-form.ts:1317](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1317)

`true` when the host contains Punycode (`xn--`) — warn the user. (R-20.7-x)

***

### warnings

> **warnings**: `string`[]

Defined in: [protocol/elicitation-form.ts:1319](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/elicitation-form.ts#L1319)

Warnings to display about ambiguous/suspicious aspects of the URL. (R-20.7-x)
