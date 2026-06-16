# TypeScript Reference SDK — Open Conformance Items (self‑contained)

**Scope:** `ts-sdk/` (`@stackific/mcp-sdk-ts`) in worktree `.worktrees/ts-reference-grade`
(off `origin/main` @ `9ed15e6`). **Baseline: `cd ts-sdk && pnpm typecheck && pnpm test` →
typecheck clean, 79 files / 3013 tests passing, 0 failures.** Grading is **strict‑runtime**:
a MUST/SHOULD whose validator exists but is never *called* by the live runtime counts as
unenforced.

> **Self‑contained — the developer does NOT need (and is not given) rubric access.** Every
> item states the requirement in plain English plus the file·line, the fix, and the test.
> Pruned to the **2 stories not yet fully Conforming**; the **44 Conforming stories are
> removed** (IDs at the bottom). Nothing here is committed/pushed.

## Status — all gating MUSTs pass; 0 Non‑Conforming

| Tier | Count | Stories |
|---|---|---|
| **Substantially** (all MUSTs pass; ≥50% SHOULDs) | **2** | S17, S39 |
| **Conforming** (pruned, no action) | 44 | — bottom |
| Non‑Conforming / Minimally | 0 | — |

Both remaining items are at the Conforming boundary — each unmet SHOULD is either a **host/
application concern** the library can't carry out or a **low‑value advisory**. If those are
deemed N/A (a reasonable call for an SDK), both become Conforming with no code change.

---

## S39 — Tasks (Substantially, 2/3 SHOULDs)

All MUSTs pass; poll‑cadence (honor/adopt the task's `pollIntervalMs`) and continue‑until‑terminal
are wired + e2e‑tested. The single remaining SHOULD:

- [ ] **Durable `taskId` persistence across restart (host concern — likely N/A).**
  *Requirement:* a client should durably persist a long‑running task's `taskId` so it can
  resume polling after a process restart. *Today:* `ts-sdk/src/client/client.ts:591`
  (`pollTaskUntilTerminal`) holds the `taskId` only in memory; there's no durable‑store hook.
  *Why it's borderline:* the SDK cannot choose a persistence backend for the host — durable
  storage is intrinsically an application responsibility. *Action:* either **mark N/A**
  (host‑owned) → S39 is Conforming, or expose a small `taskStore?` hook the host can supply and
  document it. Likely **no runtime code** beyond an optional hook.

## S17 — Multi‑Round‑Trip (Substantially)

All MUSTs pass. Two SHOULD gaps, both low‑stakes:

- [ ] **MRTR load‑shedding / backoff not driven by the live client driver.**
  *Requirement:* under load a server may return a "re‑request later" (load‑shedding) result
  instead of holding an outstanding input request, and the client should **back off** before
  retrying. *Today:* the SDK ships the helpers (`isLoadSheddingResult`,
  `computeRetryBackoffMs`, `buildReRequestInputRequiredResult` in
  `ts-sdk/src/protocol/multi-round-trip.ts`) but the live client MRTR driver doesn't apply the
  backoff path end‑to‑end. *Fix (optional):* have the client's input‑required retry path honor
  a load‑shedding result and wait `computeRetryBackoffMs(...)` before retrying; add a
  client‑driver test asserting the backoff. Low value.
- [ ] **Prefer non‑deprecated input‑request kinds (host decision — N/A).**
  *Requirement:* when more than one input‑request kind could satisfy a need, prefer a
  non‑deprecated one (e.g. elicitation over the deprecated sampling/roots kinds). *Today:* the
  SDK exposes `elicitInput`/`createMessage`/`listRoots` equally and emits a deprecation warning
  when a deprecated kind is solicited (`server.ts`), but **which** kind to solicit is the host
  tool‑handler's choice, not the library's. Reasonably **N/A** for a library.

---

## How to verify (no rubric needed — requirements stated inline)
1. `cd ts-sdk && pnpm typecheck && pnpm test` stays green (≥ 3013) after any change.
2. Any code fix ships an **end‑to‑end** test driving the real `Client` / `McpServer.dispatch`
   pipeline that **fails before / passes after** — not an isolated helper test.
3. No commit/push/PR changes without explicit permission.

## Resolved since the prior report (now Conforming — no action)
- **S14** — client now logs a warning naming each tool dropped for a malformed `Mcp-Param-*`
  annotation (RC‑1), and on a `-32001` missing‑required‑header rejection it re‑fetches
  `tools/list` and retries once (RC‑3); both e2e‑tested → 4/4 SHOULDs.
- **S40** — `pollTaskUntilTerminal` now adopts the task's latest `pollIntervalMs`
  (`adoptLatestPollIntervalMs`/`isPollingTerminalResponse`) → 10/11 SHOULDs.
- **S15** — the unmet SHOULDs (HTTP‑intermediary version‑trust; deprecated dual‑hosting / legacy
  fallback) are N/A for an endpoint server SDK (the handler's own docstring scopes them out) →
  the 7 applicable SHOULDs all pass.
- **Latent bug fixed** — `sanitizeFilePath` now rejects a NUL byte via `includes('\x00')`
  (`protocol/security.ts:1576`); it previously checked `includes(' ')` (a space).

## Pruned — Conforming (44, no action)
All stories except S17 and S39. (Earlier passes brought S30/S33 — the elicitation‑mode and
`sampling.tools` gates — and S14/S15/S16/S18/S40/S44/S45 to Conforming, each verified wired with
an end‑to‑end test.)
