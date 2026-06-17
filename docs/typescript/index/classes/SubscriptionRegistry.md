[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SubscriptionRegistry

# Class: SubscriptionRegistry

Defined in: [protocol/streaming.ts:727](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L727)

Routes incoming subscription notifications to the correct active `Subscription`
by `io.modelcontextprotocol/subscriptionId` — essential on stdio where all
subscriptions share one channel, and supported on HTTP where the key is still
present. Holds NO state across connections; closing a subscription removes it.
(§10.4, R-10.4-c, R-10.4-d, R-10.7-d)

A client MAY hold multiple independent subscriptions concurrently, each keyed by
its own request `id`. (R-10.1-i)

## Example

```ts
const registry = new SubscriptionRegistry();
registry.add(new Subscription(1, f1));
registry.add(new Subscription('two', f2));
const target = registry.route(incoming.params); // by subscriptionId
```

## Constructors

### Constructor

> **new SubscriptionRegistry**(): `SubscriptionRegistry`

#### Returns

`SubscriptionRegistry`

## Accessors

### size

#### Get Signature

> **get** **size**(): `number`

Defined in: [protocol/streaming.ts:772](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L772)

Number of currently active subscriptions.

##### Returns

`number`

***

### activeIds

#### Get Signature

> **get** **activeIds**(): readonly `string`[]

Defined in: [protocol/streaming.ts:777](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L777)

Snapshot of all active subscription ids.

##### Returns

readonly `string`[]

## Methods

### add()

> **add**(`subscription`): `void`

Defined in: [protocol/streaming.ts:735](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L735)

Registers `subscription`, keyed by its subscription id.

#### Parameters

##### subscription

[`Subscription`](Subscription.md)

#### Returns

`void`

#### Throws

when a subscription with the same id is already active (ids are
  request ids and MUST be unique while in-flight).

***

### get()

> **get**(`subscriptionId`): [`Subscription`](Subscription.md) \| `undefined`

Defined in: [protocol/streaming.ts:745](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L745)

Returns the active subscription with `subscriptionId`, or `undefined`.

#### Parameters

##### subscriptionId

`string`

#### Returns

[`Subscription`](Subscription.md) \| `undefined`

***

### route()

> **route**(`params`): [`Subscription`](Subscription.md) \| `undefined`

Defined in: [protocol/streaming.ts:754](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L754)

Routes a notification's `params` to its owning subscription using the
`io.modelcontextprotocol/subscriptionId` key. Returns `undefined` when the key
is absent or no matching subscription is active. (R-10.4-c)

#### Parameters

##### params

`unknown`

#### Returns

[`Subscription`](Subscription.md) \| `undefined`

***

### remove()

> **remove**(`subscriptionId`, `reason`): `boolean`

Defined in: [protocol/streaming.ts:763](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/streaming.ts#L763)

Closes and removes the subscription with `subscriptionId` (no retained state).
Returns `true` when one was removed. (R-10.7-d)

#### Parameters

##### subscriptionId

`string`

##### reason

[`SubscriptionCloseReason`](../type-aliases/SubscriptionCloseReason.md)

#### Returns

`boolean`
