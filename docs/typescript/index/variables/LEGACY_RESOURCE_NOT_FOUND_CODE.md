[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / LEGACY\_RESOURCE\_NOT\_FOUND\_CODE

# Variable: LEGACY\_RESOURCE\_NOT\_FOUND\_CODE

> `const` **LEGACY\_RESOURCE\_NOT\_FOUND\_CODE**: `-32002`

Defined in: [protocol/resources-read.ts:97](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L97)

The LEGACY resource-not-found code, `-32002`. An earlier protocol revision
used this code for the not-found condition; for interoperability a client
SHOULD treat it as resource-not-found in ADDITION to `-32602`. A modern
server MUST NOT mint it — [buildResourceNotFoundError](../functions/buildResourceNotFoundError.md) emits `-32602`.
(§17.6, R-17.6-c)
