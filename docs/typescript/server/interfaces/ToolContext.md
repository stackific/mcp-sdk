[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / ToolContext

# Interface: ToolContext

Defined in: [server/server.ts:130](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L130)

The ergonomic context passed to every tool handler.

## Properties

### meta

> **meta**: `Record`\<`string`, `unknown`\>

Defined in: [server/server.ts:131](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L131)

***

### signal

> **signal**: `AbortSignal`

Defined in: [server/server.ts:132](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L132)

***

### authInfo?

> `optional` **authInfo?**: `unknown`

Defined in: [server/server.ts:133](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L133)

***

### progressToken?

> `optional` **progressToken?**: `string` \| `number`

Defined in: [server/server.ts:134](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L134)

***

### taskRequested

> **taskRequested**: `boolean`

Defined in: [server/server.ts:136](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L136)

Whether the caller's params requested this call run as a task.

***

### taskTtlMs?

> `optional` **taskTtlMs?**: `number`

Defined in: [server/server.ts:137](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L137)

## Methods

### log()

> **log**(`level`, `message`): `void`

Defined in: [server/server.ts:139](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L139)

Emits a `notifications/message` at or above the server's current log level.

#### Parameters

##### level

`string`

##### message

`string`

#### Returns

`void`

***

### notify()

> **notify**(`notification`): `void`

Defined in: [server/server.ts:140](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L140)

#### Parameters

##### notification

###### method

`string`

###### params?

`Record`\<`string`, `unknown`\>

#### Returns

`void`

***

### elicitInput()

> **elicitInput**(`params`): `Promise`\<`Record`\<`string`, `unknown`\>\>

Defined in: [server/server.ts:142](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L142)

Solicits structured input from the user (server→client `elicitation/create`).

#### Parameters

##### params

`Record`\<`string`, `unknown`\>

#### Returns

`Promise`\<`Record`\<`string`, `unknown`\>\>

***

### createMessage()

> **createMessage**(`params`): `Promise`\<`Record`\<`string`, `unknown`\>\>

Defined in: [server/server.ts:144](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L144)

Borrows the client's model (server→client `sampling/createMessage`, Deprecated).

#### Parameters

##### params

`Record`\<`string`, `unknown`\>

#### Returns

`Promise`\<`Record`\<`string`, `unknown`\>\>

***

### listRoots()

> **listRoots**(): `Promise`\<`Record`\<`string`, `unknown`\>\>

Defined in: [server/server.ts:146](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L146)

Requests the client's workspace roots (server→client `roots/list`, Deprecated).

#### Returns

`Promise`\<`Record`\<`string`, `unknown`\>\>

***

### sendToolListChanged()

> **sendToolListChanged**(): `void`

Defined in: [server/server.ts:147](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L147)

#### Returns

`void`

***

### sendPromptListChanged()

> **sendPromptListChanged**(): `void`

Defined in: [server/server.ts:148](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L148)

#### Returns

`void`

***

### sendResourceListChanged()

> **sendResourceListChanged**(): `void`

Defined in: [server/server.ts:149](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L149)

#### Returns

`void`

***

### sendResourceUpdated()

> **sendResourceUpdated**(`params`): `void`

Defined in: [server/server.ts:150](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L150)

#### Parameters

##### params

###### uri

`string`

#### Returns

`void`

***

### notifySubscribers()

> **notifySubscribers**(`notification`): `void`

Defined in: [server/server.ts:152](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/server.ts#L152)

Broadcasts a change notification to all matching subscription streams (§10.5/§10.6).

#### Parameters

##### notification

###### method

`string`

###### params?

`Record`\<`string`, `unknown`\>

#### Returns

`void`
