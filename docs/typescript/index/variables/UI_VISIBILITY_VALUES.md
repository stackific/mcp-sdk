[**@stackific/mcp-sdk**](../../README.md)

***

[@stackific/mcp-sdk](../../README.md) / [index](../README.md) / UI\_VISIBILITY\_VALUES

# Variable: UI\_VISIBILITY\_VALUES

> `const` **UI\_VISIBILITY\_VALUES**: readonly \[`"model"`, `"app"`\]

Defined in: [protocol/ui.ts:424](https://github.com/stackific/mcp-sdk-v2/blob/main/ts-sdk/src/protocol/ui.ts#L424)

The exact visibility enum strings: which actor may invoke a tool. (§26.3,
R-26.3-d)

  - `"model"` — callable by the model/agent via ordinary tool-calling (§16);
  - `"app"`   — callable by the rendered UI over the channel (§26.5).
