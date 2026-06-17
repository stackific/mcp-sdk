[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / TaskStore

# Interface: TaskStore

Defined in: [server/server.ts:118](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L118)

The minimal task store the dispatcher needs for the Tasks extension (§25).

## Methods

### get()

> **get**(`taskId`): `object`

Defined in: [server/server.ts:119](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L119)

#### Parameters

##### taskId

`string`

#### Returns

`object`

##### status

> **status**: `string`

***

### getDetailed()

> **getDetailed**(`taskId`): `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:121](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L121)

The status-appropriate DetailedTask (status + inline result/error/inputRequests). (§25.7)

#### Parameters

##### taskId

`string`

#### Returns

`Record`\<`string`, `unknown`\>

***

### cancel()

> **cancel**(`taskId`): `object`

Defined in: [server/server.ts:122](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L122)

#### Parameters

##### taskId

`string`

#### Returns

`object`

##### status

> **status**: `string`

***

### applyInput()

> **applyInput**(`taskId`, `inputResponses`): `unknown`

Defined in: [server/server.ts:124](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L124)

Supplies input to an input_required task. (§25.8)

#### Parameters

##### taskId

`string`

##### inputResponses

`Record`\<`string`, `unknown`\>

#### Returns

`unknown`

***

### setUpdateListener()?

> `optional` **setUpdateListener**(`listener`): `void`

Defined in: [server/server.ts:126](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L126)

Registers a listener invoked with the new DetailedTask on every status change. (§25.10)

#### Parameters

##### listener

(`task`) => `void`

#### Returns

`void`
