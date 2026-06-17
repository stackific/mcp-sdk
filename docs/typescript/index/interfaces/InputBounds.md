[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / InputBounds

# Interface: InputBounds

Defined in: [protocol/security.ts:1466](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L1466)

Resource bounds a receiver imposes while validating peer inputs. (§28.10, R-28.10-k, R-28.10-l)

## Properties

### maxSchemaDepth

> **maxSchemaDepth**: `number`

Defined in: [protocol/security.ts:1468](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L1468)

Maximum schema nesting depth; deeper schemas are rejected. (R-28.10-k)

***

### maxPayloadBytes

> **maxPayloadBytes**: `number`

Defined in: [protocol/security.ts:1470](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/security.ts#L1470)

Maximum serialized payload size in bytes; larger inputs are rejected. (R-28.10-l)
