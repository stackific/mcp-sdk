[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / ResourceTemplateDef

# Interface: ResourceTemplateDef

Defined in: [server/server.ts:186](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L186)

## Extends

- [`ResourceDef`](ResourceDef.md)

## Properties

### title?

> `optional` **title?**: `string`

Defined in: [server/server.ts:176](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L176)

#### Inherited from

[`ResourceDef`](ResourceDef.md).[`title`](ResourceDef.md#title)

***

### description?

> `optional` **description?**: `string`

Defined in: [server/server.ts:177](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L177)

#### Inherited from

[`ResourceDef`](ResourceDef.md).[`description`](ResourceDef.md#description)

***

### mimeType?

> `optional` **mimeType?**: `string`

Defined in: [server/server.ts:178](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L178)

#### Inherited from

[`ResourceDef`](ResourceDef.md).[`mimeType`](ResourceDef.md#mimetype)

***

### uriTemplate

> **uriTemplate**: `string`

Defined in: [server/server.ts:187](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L187)

***

### complete?

> `optional` **complete?**: `Record`\<`string`, (`value`) => `string`[]\>

Defined in: [server/server.ts:189](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L189)

Completion callbacks per template variable.
