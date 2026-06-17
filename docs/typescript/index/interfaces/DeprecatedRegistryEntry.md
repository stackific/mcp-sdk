[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / DeprecatedRegistryEntry

# Interface: DeprecatedRegistryEntry

Defined in: [lifecycle/state.ts:52](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L52)

One row of the derived registry of deprecated features (§27.3).
The per-feature notices at the authoritative defining sections resolve conflicts.

## Properties

### feature

> **feature**: `string`

Defined in: [lifecycle/state.ts:54](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L54)

Name of the deprecated feature.

***

### definedIn

> **definedIn**: `string`

Defined in: [lifecycle/state.ts:56](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L56)

Section reference where the feature is authoritatively defined.

***

### migrationNote

> **migrationNote**: `string`

Defined in: [lifecycle/state.ts:58](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L58)

One-line migration guidance. (R-27.2-g)

***

### deprecatedSince

> **deprecatedSince**: `string`

Defined in: [lifecycle/state.ts:60](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L60)

The revision in which the feature first became Deprecated. (R-27.2)

***

### earliestRemoval

> **earliestRemoval**: `string`

Defined in: [lifecycle/state.ts:66](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/lifecycle/state.ts#L66)

Protocol revision on or after which removal is eligible. Per §27.2 this MUST
be at least 12 months after [deprecatedSince](#deprecatedsince), so the removal-window
rule can be evaluated against this row (see `lifecycle/policy.ts`). (R-27.2)
