[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / TASK\_INVALID\_PARAMS\_CODE

# Variable: TASK\_INVALID\_PARAMS\_CODE

> `const` **TASK\_INVALID\_PARAMS\_CODE**: `-32602` = `INVALID_PARAMS_CODE`

Defined in: [protocol/tasks-lifecycle.ts:115](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/tasks-lifecycle.ts#L115)

The §22 error code a server uses to answer `tasks/get` / `tasks/update` /
`tasks/cancel` for a `taskId` that is unknown — never existed, or expired and
removed: `-32602` (Invalid params). (§25.7, §25.11, R-25.7-r, R-25.8-m,
R-25.9-g, R-25.11-d)

NOTE: S39's import('./tasks.js').TASK\_NOT\_FOUND\_CODE resolves to the
SAME core `-32602` (Invalid params) — both the §25.4/§25.6 not-found condition
and these S40 wire operations specify `-32602`. The legacy `-32002` literal is
NOT minted by this SDK; this reuses [INVALID\_PARAMS\_CODE](INVALID_PARAMS_CODE.md) (S05) accordingly.
