[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / isTaskAcknowledgementResult

# Function: isTaskAcknowledgementResult()

> **isTaskAcknowledgementResult**(`value`): `value is objectOutputType<{ resultType: ZodLiteral<"complete">; _meta: ZodOptional<ZodRecord<ZodString, ZodUnknown>> }, ZodTypeAny, "passthrough">`

Defined in: [protocol/tasks-lifecycle.ts:448](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tasks-lifecycle.ts#L448)

Returns `true` when `value` is a well-formed task acknowledgement result —
`resultType: "complete"` (the shared `tasks/update` / `tasks/cancel` ack).
(R-25.8-j, R-25.9-e)

## Parameters

### value

`unknown`

## Returns

`value is objectOutputType<{ resultType: ZodLiteral<"complete">; _meta: ZodOptional<ZodRecord<ZodString, ZodUnknown>> }, ZodTypeAny, "passthrough">`
