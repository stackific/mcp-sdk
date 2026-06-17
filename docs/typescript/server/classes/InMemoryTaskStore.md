[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [server](../README.md) / InMemoryTaskStore

# Class: InMemoryTaskStore

Defined in: [server/tasks.ts:47](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L47)

A conformant, in-memory store for the Tasks extension (§25).

## Implements

- [`TaskStore`](../interfaces/TaskStore.md)

## Constructors

### Constructor

> **new InMemoryTaskStore**(`options?`): `InMemoryTaskStore`

Defined in: [server/tasks.ts:53](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L53)

#### Parameters

##### options?

[`InMemoryTaskStoreOptions`](../interfaces/InMemoryTaskStoreOptions.md) = `{}`

#### Returns

`InMemoryTaskStore`

## Methods

### createTask()

> **createTask**(`options?`): `objectOutputType`

Defined in: [server/tasks.ts:61](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L61)

Creates a task in the initial `working` state and returns the handle. (§25.3, §25.4)

#### Parameters

##### options?

###### ttlMs?

`number` \| `null`

###### taskId?

`string`

#### Returns

`objectOutputType`

***

### updateStatus()

> **updateStatus**(`taskId`, `status`, `statusMessage?`): `objectOutputType`

Defined in: [server/tasks.ts:78](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L78)

Transitions a task to `status`, enforcing the legal transition graph. (§25.5)

#### Parameters

##### taskId

`string`

##### status

`"input_required"` \| `"cancelled"` \| `"completed"` \| `"working"` \| `"failed"`

##### statusMessage?

`string`

#### Returns

`objectOutputType`

***

### setUpdateListener()

> **setUpdateListener**(`listener`): `void`

Defined in: [server/tasks.ts:95](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L95)

Registers a listener invoked with the new DetailedTask on every status change. (§25.10)

#### Parameters

##### listener

(`task`) => `void`

#### Returns

`void`

#### Implementation of

[`TaskStore`](../interfaces/TaskStore.md).[`setUpdateListener`](../interfaces/TaskStore.md#setupdatelistener)

***

### storeResult()

> **storeResult**(`taskId`, `result`, `status?`): `objectOutputType`

Defined in: [server/tasks.ts:101](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L101)

Stores the terminal payload and moves the task to a terminal status (default `completed`).

#### Parameters

##### taskId

`string`

##### result

`Record`\<`string`, `unknown`\>

##### status?

`"input_required"` \| `"cancelled"` \| `"completed"` \| `"working"` \| `"failed"`

#### Returns

`objectOutputType`

***

### get()

> **get**(`taskId`): `objectOutputType`

Defined in: [server/tasks.ts:113](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L113)

`tasks/get` — the current task handle, or `-32602` if unknown/expired. (§25.7)

#### Parameters

##### taskId

`string`

#### Returns

`objectOutputType`

#### Implementation of

[`TaskStore`](../interfaces/TaskStore.md).[`get`](../interfaces/TaskStore.md#get)

***

### getDetailed()

> **getDetailed**(`taskId`): `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\>

Defined in: [server/tasks.ts:123](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L123)

The status-appropriate [DetailedTask](../../index/type-aliases/DetailedTask.md) the `tasks/get` result wraps
(§25.7): a terminal task carries its outcome INLINE — `result` when completed,
`error` when failed — `inputRequests` when input-required, and nothing extra
while working/cancelled. (R-25.5-d)

#### Parameters

##### taskId

`string`

#### Returns

`objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\> \| `objectOutputType`\<`object` & `object`, `ZodTypeAny`, `"passthrough"`\>

#### Implementation of

[`TaskStore`](../interfaces/TaskStore.md).[`getDetailed`](../interfaces/TaskStore.md#getdetailed)

***

### storeError()

> **storeError**(`taskId`, `error`): `objectOutputType`

Defined in: [server/tasks.ts:151](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L151)

Records an inline error and moves the task to `failed`. (§25.5)

#### Parameters

##### taskId

`string`

##### error

###### code

`number`

###### message

`string`

###### data?

`unknown`

#### Returns

`objectOutputType`

***

### applyInput()

> **applyInput**(`taskId`, `inputResponses`): `objectOutputType`

Defined in: [server/tasks.ts:157](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L157)

`tasks/update` — supplies input to an `input_required` task, moving it back to `working`. (§25.8)

#### Parameters

##### taskId

`string`

##### inputResponses

`Record`\<`string`, `unknown`\>

#### Returns

`objectOutputType`

#### Implementation of

[`TaskStore`](../interfaces/TaskStore.md).[`applyInput`](../interfaces/TaskStore.md#applyinput)

***

### getResult()

> **getResult**(`taskId`): `Record`\<`string`, `unknown`\>

Defined in: [server/tasks.ts:167](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L167)

`tasks/result` — terminal payload; `-32602` if unknown/expired or not finished. (§25.7)

#### Parameters

##### taskId

`string`

#### Returns

`Record`\<`string`, `unknown`\>

***

### list()

> **list**(): `objectOutputType`\<\{ `taskId`: `ZodString`; `status`: `ZodEnum`\<\[`"working"`, `"input_required"`, `"completed"`, `"failed"`, `"cancelled"`\]\>; `statusMessage`: `ZodOptional`\<`ZodString`\>; `createdAt`: `ZodString`; `lastUpdatedAt`: `ZodString`; `ttlMs`: `ZodUnion`\<\[`ZodNumber`, `ZodNull`\]\>; `pollIntervalMs`: `ZodOptional`\<`ZodNumber`\>; \}, `ZodTypeAny`, `"passthrough"`\>[]

Defined in: [server/tasks.ts:176](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L176)

`tasks/list` — all live tasks (expired ones are discarded first).

#### Returns

`objectOutputType`\<\{ `taskId`: `ZodString`; `status`: `ZodEnum`\<\[`"working"`, `"input_required"`, `"completed"`, `"failed"`, `"cancelled"`\]\>; `statusMessage`: `ZodOptional`\<`ZodString`\>; `createdAt`: `ZodString`; `lastUpdatedAt`: `ZodString`; `ttlMs`: `ZodUnion`\<\[`ZodNumber`, `ZodNull`\]\>; `pollIntervalMs`: `ZodOptional`\<`ZodNumber`\>; \}, `ZodTypeAny`, `"passthrough"`\>[]

***

### cancel()

> **cancel**(`taskId`): `objectOutputType`

Defined in: [server/tasks.ts:182](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/server/tasks.ts#L182)

`tasks/cancel` — move a non-terminal task to `cancelled`; terminal tasks are returned unchanged. (§25.9)

#### Parameters

##### taskId

`string`

#### Returns

`objectOutputType`

#### Implementation of

[`TaskStore`](../interfaces/TaskStore.md).[`cancel`](../interfaces/TaskStore.md#cancel)
