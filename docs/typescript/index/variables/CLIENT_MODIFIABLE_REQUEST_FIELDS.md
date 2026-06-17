[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / CLIENT\_MODIFIABLE\_REQUEST\_FIELDS

# Variable: CLIENT\_MODIFIABLE\_REQUEST\_FIELDS

> `const` **CLIENT\_MODIFIABLE\_REQUEST\_FIELDS**: readonly \[`"systemPrompt"`, `"includeContext"`, `"temperature"`, `"stopSequences"`, `"metadata"`\]

Defined in: [protocol/sampling.ts:822](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/sampling.ts#L822)

Fields a client (or host) MAY modify or omit as part of its human-in-the-loop
control over a sampling request, without communicating the change to the
server. (R-21.2.10-e)
