[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / obligedSectionsForCapabilities

# Function: obligedSectionsForCapabilities()

> **obligedSectionsForCapabilities**(`advertised`): `string`[]

Defined in: [protocol/conformance-requirements.ts:412](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/conformance-requirements.ts#L412)

Returns the spec sections whose MUST-level behavior an implementation is bound
to, given the capabilities it advertises. (§29.4 item 1, R-29.4-a – R-29.4-g)
The result is deterministic, de-duplicated, and includes the additional
sections (e.g. `resources.subscribe` adds `10`).

## Parameters

### advertised

`Iterable`\<`string`\>

## Returns

`string`[]
