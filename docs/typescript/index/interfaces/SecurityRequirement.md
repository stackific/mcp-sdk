[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / SecurityRequirement

# Interface: SecurityRequirement

Defined in: [protocol/security.ts:95](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L95)

A single normative §28 requirement, as consolidated by S44.

## Properties

### id

> **id**: `string`

Defined in: [protocol/security.ts:97](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L97)

The requirement-atom id, e.g. `'R-28.3-g'`.

***

### level

> **level**: [`SecurityRequirementLevel`](../type-aliases/SecurityRequirementLevel.md)

Defined in: [protocol/security.ts:99](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L99)

Its normative strength.

***

### section

> **section**: `string`

Defined in: [protocol/security.ts:101](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L101)

The §28 subsection that states it, e.g. `'§28.3'`.

***

### principle

> **principle**: `"user-consent-and-control"` \| `"data-privacy"` \| `"tool-safety"` \| `"host-mediated-trust"`

Defined in: [protocol/security.ts:103](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L103)

The core principle it derives from. (§28.1)

***

### statement

> **statement**: `string`

Defined in: [protocol/security.ts:105](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L105)

A concise restatement of the obligation.
