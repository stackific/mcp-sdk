#!/usr/bin/env bash
#
# GHAS pre-push gate.
#
# Runs the local CodeQL "security-and-quality" scan for every language (via
# `task ghas`) and fails when a finding is NOT present in the accepted baseline
# (.codeql-baseline.txt). This is what lets the heavy CodeQL suite act as a gate
# without blocking on the findings we have deliberately accepted (dismissed on
# GitHub but still present in the code).
#
#   scripts/ghas-gate.sh            scan, then FAIL on any finding not baselined
#   scripts/ghas-gate.sh --update   scan, then (re)write the baseline
#
# A finding is keyed by:  ruleId | file | primaryLocationLineHash
# The line hash is CodeQL's own content fingerprint, so the key survives line
# moves and is invalidated only when the flagged code itself changes.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

BASELINE="$ROOT/.codeql-baseline.txt"
SARIFS=(.codeql/js.sarif .codeql/py.sarif .codeql/cs.sarif)

mode="check"
[ "${1:-}" = "--update" ] && mode="update"

# Build the CodeQL databases and analyze — writes the SARIFs under .codeql/.
task ghas

fingerprints() {
  local f
  for f in "${SARIFS[@]}"; do
    [ -f "$f" ] || continue
    jq -r '
      .runs[].results[]
      | [ (.ruleId // .rule.id // "?"),
          (.locations[0].physicalLocation.artifactLocation.uri // "?"),
          (.partialFingerprints.primaryLocationLineHash // "?")
        ] | join("|")
    ' "$f"
  done | grep -v '^$' | LC_ALL=C sort -u
}

current="$(fingerprints)"

if [ "$mode" = "update" ]; then
  printf '%s\n' "$current" > "$BASELINE"
  echo "✅ GHAS baseline written: $(printf '%s\n' "$current" | grep -c .) accepted finding(s) → ${BASELINE#"$ROOT"/}"
  exit 0
fi

if [ ! -f "$BASELINE" ]; then
  echo "❌ No GHAS baseline at ${BASELINE#"$ROOT"/}. Generate one with: task ghas:baseline" >&2
  exit 1
fi

new="$(LC_ALL=C comm -23 <(printf '%s\n' "$current") <(LC_ALL=C sort -u "$BASELINE"))"

if [ -n "$new" ]; then
  count="$(printf '%s\n' "$new" | grep -c .)"
  echo "❌ GHAS: $count new CodeQL finding(s) not in the accepted baseline:" >&2
  printf '%s\n' "$new" | sed 's/^/   /' >&2
  echo >&2
  echo "Fix them, or — if intentional — dismiss the alert on GitHub and refresh" >&2
  echo "the baseline with: task ghas:baseline" >&2
  exit 1
fi

echo "✅ GHAS: no new CodeQL findings beyond the accepted baseline ($(grep -c . "$BASELINE") known)."
