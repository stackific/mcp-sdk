[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [client](../README.md) / ClientOptions

# Interface: ClientOptions

Defined in: [client/client.ts:102](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L102)

Construction options for [Client](../classes/Client.md).

## Properties

### capabilities?

> `optional` **capabilities?**: `Record`\<`string`, `unknown`\>

Defined in: [client/client.ts:104](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L104)

Capabilities declared in every request's `_meta`. (§6.2) Defaults to `{}`.

***

### protocolVersions?

> `optional` **protocolVersions?**: `string`[]

Defined in: [client/client.ts:106](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L106)

Acceptable protocol revisions, most-preferred first. Defaults to `[CURRENT_PROTOCOL_VERSION]`.

***

### logger?

> `optional` **logger?**: `object`

Defined in: [client/client.ts:112](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/client/client.ts#L112)

Optional sink for advisory client warnings — e.g. a `tools/list` tool dropped
because its `x-mcp-header` annotation is invalid (§9.5.1, R-9.5.1-k). Injected
rather than a hard `console` dependency so the SDK stays edge-safe/testable.

#### warn()

> **warn**(`message`): `void`

##### Parameters

###### message

`string`

##### Returns

`void`
