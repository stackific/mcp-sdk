[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / PKCE\_UNRESERVED\_RE

# Variable: PKCE\_UNRESERVED\_RE

> `const` **PKCE\_UNRESERVED\_RE**: `RegExp`

Defined in: [protocol/authorization-flow.ts:105](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/authorization-flow.ts#L105)

The RFC 7636 `code_verifier` "unreserved" alphabet:
`ALPHA / DIGIT / "-" / "." / "_" / "~"`. A verifier MUST consist solely of
these characters. (R-23.5-b)
