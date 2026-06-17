# Documentation

The MCP V2 RC specification plus auto-generated API references for all three SDKs.

The API docs are generated **from source** as Markdown (browsable directly on GitHub).
Regenerate them with `task docs` — or per language: `task docs:ts`, `task docs:py`,
`task docs:csharp`.

## Specification

- [**MCP V2 RC specification**](model-context-protocol.md) — the normative protocol
  document (wire revision `2026-07-28`).

## SDK API reference

| SDK            | Package / namespace                  | Generated docs                                  | Generator         |
| -------------- | ------------------------------------ | ----------------------------------------------- | ----------------- |
| **TypeScript** | `@stackific/mcp-sdk`                 | [`typescript/`](typescript/README.md)           | TypeDoc           |
| **Python**     | `stackific-mcp` (`stackific.mcp`)    | [`python/`](python/stackific.mcp.md)            | pydoc-markdown    |
| **C#**         | `Stackific.Mcp`                      | [`csharp/`](csharp/Stackific.Mcp.md)            | XmlDocMarkdown    |

All three SDKs implement the same specification; the references above are produced from
each SDK's own in-source documentation, so they stay in lock-step with the code.
