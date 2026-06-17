[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / ConsentGrant

# Interface: ConsentGrant

Defined in: [protocol/security.ts:311](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L311)

A record of the consent a user has explicitly granted for a single operation,
the host's consent-gate state. (§28.2) Absence of a record is NOT consent
(R-28.2-d); the scope captured here is what a later operation is compared
against for material change (R-28.2-e, R-28.2-f).

## Properties

### operation

> **operation**: `string`

Defined in: [protocol/security.ts:313](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L313)

The operation the user authorized, e.g. a tool name or `'resource-exposure'`.

***

### scope

> **scope**: `string`

Defined in: [protocol/security.ts:320](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L320)

An opaque, comparable summary of WHAT was authorized — the data scope and the
action. A materially different value on a later request means fresh consent is
required (R-28.2-e, R-28.2-f). Callers choose a stable serialization (e.g. the
sorted argument keys + sensitivity class).

***

### informed

> **informed**: `boolean`

Defined in: [protocol/security.ts:322](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L322)

`true` when the user actively, informedly granted it. Defaults to `false` if absent. (R-28.2-b)
