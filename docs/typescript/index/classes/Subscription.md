[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / Subscription

# Class: Subscription

Defined in: [protocol/streaming.ts:593](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L593)

Tracks the request-scoped lifecycle of a single subscription. The state is scoped
to the `subscriptions/listen` request, NOT to the connection: once `close()` is
reached the subscription is gone and retains NO resumable state. (§10.7)

Lifecycle: `opening` → (ack sent) → `active` → (cancel/teardown/transport-close)
→ `closed`. There is no resumption; re-establishment is a NEW `subscriptions/listen`
request yielding a NEW id. (R-10.7-d, R-10.7-f)

## Example

```ts
const sub = new Subscription(1, requested, serverCaps);
const ackParams = sub.acknowledge();            // → 'active', honored subset + subId
sub.close('client-cancel');                     // → 'closed'
```

## Constructors

### Constructor

> **new Subscription**(`requestId`, `requested`, `serverCaps?`, `options?`): `Subscription`

Defined in: [protocol/streaming.ts:607](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L607)

#### Parameters

##### requestId

`string` \| `number`

The `subscriptions/listen` request `id`.

##### requested

`objectOutputType`

The client's requested `SubscriptionFilter`.

##### serverCaps?

`Record`\<`string`, `unknown`\> = `{}`

The server's declared capabilities (gates the honored subset).

##### options?

[`AcknowledgedFilterOptions`](../interfaces/AcknowledgedFilterOptions.md) = `{}`

Extension-activation refinements (e.g. `tasksActive`, §25.10).

#### Returns

`Subscription`

## Properties

### subscriptionId

> `readonly` **subscriptionId**: `string`

Defined in: [protocol/streaming.ts:595](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L595)

The subscription identifier: the request `id` serialized as a JSON string.

***

### acknowledgedFilter

> `readonly` **acknowledgedFilter**: `objectOutputType`

Defined in: [protocol/streaming.ts:597](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L597)

The honored-subset filter the server agreed to (computed at construction).

***

### requestId

> `readonly` **requestId**: `string` \| `number`

Defined in: [protocol/streaming.ts:608](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L608)

The `subscriptions/listen` request `id`.

***

### requested

> `readonly` **requested**: `objectOutputType`

Defined in: [protocol/streaming.ts:609](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L609)

The client's requested `SubscriptionFilter`.

## Accessors

### state

#### Get Signature

> **get** **state**(): [`SubscriptionState`](../type-aliases/SubscriptionState.md)

Defined in: [protocol/streaming.ts:618](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L618)

Current lifecycle state.

##### Returns

[`SubscriptionState`](../type-aliases/SubscriptionState.md)

***

### closeReason

#### Get Signature

> **get** **closeReason**(): [`SubscriptionCloseReason`](../type-aliases/SubscriptionCloseReason.md) \| `undefined`

Defined in: [protocol/streaming.ts:623](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L623)

How the subscription closed, or `undefined` while still open.

##### Returns

[`SubscriptionCloseReason`](../type-aliases/SubscriptionCloseReason.md) \| `undefined`

***

### isClosed

#### Get Signature

> **get** **isClosed**(): `boolean`

Defined in: [protocol/streaming.ts:704](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L704)

Returns `true` once the subscription has closed.

##### Returns

`boolean`

## Methods

### acknowledge()

> **acknowledge**(): `objectOutputType`

Defined in: [protocol/streaming.ts:634](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L634)

Builds the mandatory first message — the `notifications/subscriptions/acknowledged`
params — and transitions `opening` → `active`. The acknowledgement carries the
honored subset and the subscription id in `_meta`. (R-10.1-e, R-10.3-a, R-10.3-e)

#### Returns

`objectOutputType`

#### Throws

when called after the subscription has already acknowledged or closed.

***

### metaFragment()

> **metaFragment**(): `Record`\<`string`, `unknown`\> & `object`

Defined in: [protocol/streaming.ts:651](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L651)

Returns the `params._meta` fragment to attach to a change notification on this
stream — carrying the subscription id. (R-10.4-a, R-10.5-a)

#### Returns

***

### mayEmit()

> **mayEmit**(`method`, `updatedUri?`): `boolean`

Defined in: [protocol/streaming.ts:660](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L660)

Returns `true` when the server MAY emit change notification `method` on this
subscription's stream (state `active` and the acknowledged filter permits it).
For `notifications/resources/updated`, pass `updatedUri`. (R-10.5-l)

#### Parameters

##### method

`string`

##### updatedUri?

`string`

#### Returns

`boolean`

***

### close()

> **close**(`reason`): `void`

Defined in: [protocol/streaming.ts:671](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L671)

Transitions to `closed` for the given reason. Idempotent: a second close is a
no-op (the first reason wins). After close the subscription retains no state and
is NOT resumable — recovery requires a new `subscriptions/listen`. (R-10.7-a,
R-10.7-b, R-10.7-c, R-10.7-d, R-10.7-f)

#### Parameters

##### reason

[`SubscriptionCloseReason`](../type-aliases/SubscriptionCloseReason.md)

#### Returns

`void`

***

### teardownNotification()

> **teardownNotification**(`reason?`): `object`

Defined in: [protocol/streaming.ts:691](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L691)

Builds the server-teardown signal for this subscription: a
`notifications/cancelled` referencing the `subscriptions/listen` request `id`.

§10.7 (R-10.7-b) requires a server tearing down a subscription (e.g. during
shutdown) to signal it to the client — on **stdio** by sending this
notification, on **Streamable HTTP** by closing the `text/event-stream`
response. This `Subscription` is transport-agnostic: the stdio transport
sends the value returned here after `close('server-teardown')`, while the HTTP
transport simply ends the SSE response. The `params.requestId` always equals
this subscription's listen `id` so the client can correlate the teardown.

#### Parameters

##### reason?

`string` = `'subscription torn down by server'`

OPTIONAL human-readable explanation.

#### Returns

`object`

##### jsonrpc

> **jsonrpc**: `"2.0"`

##### method

> **method**: `"notifications/cancelled"`

##### params

> **params**: `object`

###### params.requestId

> **requestId**: `string` \| `number`

###### params.reason

> **reason**: `string`
