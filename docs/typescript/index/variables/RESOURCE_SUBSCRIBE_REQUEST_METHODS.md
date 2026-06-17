[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / RESOURCE\_SUBSCRIBE\_REQUEST\_METHODS

# Variable: RESOURCE\_SUBSCRIBE\_REQUEST\_METHODS

> `const` **RESOURCE\_SUBSCRIBE\_REQUEST\_METHODS**: readonly \[\]

Defined in: [protocol/resources-read.ts:522](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/resources-read.ts#L522)

There is NO `subscribe` / `unsubscribe` request method for resources;
subscription is governed ENTIRELY by the §10 / S16 stream filters. This
constant records that absence so a caller can assert it. (§17.7, R-17.7-a)
