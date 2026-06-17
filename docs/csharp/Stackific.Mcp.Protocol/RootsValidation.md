# RootsValidation class

The §21.1.5 behavioral layer for the Deprecated Roots capability — the C# counterpart of the TypeScript `protocol/roots.ts` validation helpers. The wire records (Root, ListRootsResult) stay permissive so any well-formed payload round-trips; this static class adds the §21.1.5 MUST/SHOULD checks the spec layers on top: the `file://` + RFC 3986 `uri` constraint (R-21.1.5-b/d), the path-traversal guard (R-21.1.5-i), the consent/scope assembly pipeline (R-21.1.5-g/h), the non-`file`-scheme disposition (R-21.1.5-c), and the server-side derived-path containment check (R-21.1.5-k).

```csharp
public static class RootsValidation
```

## Public Members

| name | description |
| --- | --- |
| const [ProtocolEnforcesRootBoundaries](RootsValidation/ProtocolEnforcesRootBoundaries.md) | `false` — a server MUST NOT assume the protocol enforces root boundaries on its behalf; roots are informational guidance, not access control (R-21.1.5-l; AC-32.18). |
| const [RootsListChangedNotificationMethod](RootsValidation/RootsListChangedNotificationMethod.md) | The `notifications/roots/list_changed` method name. ⚠️ UNSUPPORTED in this revision: no `listChanged` sub-flag is defined for `roots`, so a client MUST NOT rely on it (R-21.1.2-c; AC-32.5). Named only so a receiver can recognize and ignore it. |
| const [RootsListChangedSupported](RootsValidation/RootsListChangedSupported.md) | `false` — this revision defines NO `listChanged` mechanism for `roots` (R-21.1.2-c; AC-32.5). |
| const [RootsListMethod](RootsValidation/RootsListMethod.md) | The exact `roots/list` method string; MUST match exactly, case-sensitively (R-21.1.4-a; AC-32.8). |
| static [ApplyNonFileDisposition](RootsValidation/ApplyNonFileDisposition.md)(…) | Applies a receiver's chosen *disposition* to a candidate root *uri* that does NOT use the `file` scheme, returning whether the root is kept. A valid `file://` URI is always kept; a non-`file` URI is dropped under EITHER disposition (they differ only in whether the receiver surfaces an error elsewhere). Mirrors TS `applyNonFileDisposition`. (R-21.1.5-c; AC-32.12) |
| static [AssembleListRootsResult](RootsValidation/AssembleListRootsResult.md)(…) | Assembles a ListRootsResult a client supplies on retry, enforcing the client-side consent, scope, and validation obligations. A root is INCLUDED only when it is in-scope (R-21.1.5-g), consented (R-21.1.5-h), URI-valid (R-21.1.5-b/d), AND traversal-safe (R-21.1.5-i); every excluded candidate is reported with its reason. When nothing qualifies the result is the conformant empty listing. Mirrors TS `assembleListRootsResult`. (R-21.1.5-a, -g, -h, -i; AC-32.10, AC-32.15, AC-32.16) |
| static [IsConformantNonFileDisposition](RootsValidation/IsConformantNonFileDisposition.md)(…) | Returns `true` when *disposition* is a CONFORMANT way to handle a root whose `uri` is not `file`-scheme: a receiver MAY either Reject it or Ignore it. Mirrors TS `isConformantNonFileDisposition` (here the enum already constrains the domain, so this always returns `true`). (R-21.1.5-c; AC-32.12) |
| static [IsPathTraversalSafe](RootsValidation/IsPathTraversalSafe.md)(…) | Returns `true` when *uri*, after passing [`IsValidFileUri`](./RootsValidation/IsValidFileUri.md), shows NO path-traversal artifacts — no `..` path segment and no percent-encoded `..` (`%2e%2e`, case-insensitively). Mirrors TS `isPathTraversalSafe`. (R-21.1.5-i; AC-32.16) |
| static [IsPathWithinReportedRoots](RootsValidation/IsPathWithinReportedRoots.md)(…) | Validates a server-derived filesystem path against the reported roots, so the server does NOT rely on protocol-level enforcement. Returns `true` only when *derivedUri* is a valid `file://` URI whose path is contained within (equal to, or a descendant of) at least one reported root's path. Containment compares decoded path segments (so `/a/b` contains `/a/b/c` but not `/a/bc`); roots whose own `uri` is invalid are skipped. Mirrors TS `isPathWithinReportedRoots`. (R-21.1.5-k, R-21.1.5-l; AC-32.18) |
| static [IsRootsListMethod](RootsValidation/IsRootsListMethod.md)(…) | Returns `true` when *value* is EXACTLY `"roots/list"` (case-sensitive). A value differing only in case (for example `"Roots/List"`) is NOT valid. Mirrors TS `isRootsListMethod`. (R-21.1.4-a; AC-32.8) |
| static [IsValidFileUri](RootsValidation/IsValidFileUri.md)(…) | Returns `true` when *uri* is a syntactically valid URI per RFC 3986 AND uses the `file` scheme (begins with `file://`). A non-`file` scheme, an empty value, or a malformed URI all return `false`. Mirrors TS `isValidFileUri`. (R-21.1.5-b, R-21.1.5-d; AC-32.11) |
| static [IsValidRoot](RootsValidation/IsValidRoot.md)(…) | Returns `true` when *root* satisfies the §21.1 `uri` constraints: a present `uri` that is a valid `file://` RFC 3986 URI. Mirrors TS `parseRoot` (the success branch). (R-21.1.5-b, R-21.1.5-d; AC-32.11) |
| static [IsValidStrictListRootsResult](RootsValidation/IsValidStrictListRootsResult.md)(…) | Returns `true` when *result* is a valid strict ListRootsResult: `roots` is present (MAY be empty) and every entry has a valid `file://``uri`. Mirrors TS `parseStrictListRootsResult`. (R-21.1.5-a, R-21.1.5-b, R-21.1.5-d; AC-32.10, AC-32.11) |
| static [MayRelyOnRootsListChanged](RootsValidation/MayRelyOnRootsListChanged.md)(…) | Returns `false` for every input — a client MUST NOT rely on a `listChanged`-style mechanism for roots in this revision, regardless of capability contents. Mirrors TS `mayRelyOnRootsListChanged`. (R-21.1.2-c; AC-32.5) |
| static [ProtocolEnforcesRootBoundariesFn](RootsValidation/ProtocolEnforcesRootBoundariesFn.md)() | Returns `false` — confirms the protocol does NOT enforce root boundaries; a server MUST validate derived paths itself. Mirrors TS `protocolEnforcesRootBoundaries`. (R-21.1.5-l; AC-32.18) |
| static [ShouldTolerateUnavailableRoot](RootsValidation/ShouldTolerateUnavailableRoot.md)(…) | Returns `true` — a server SHOULD tolerate a previously-reported root that has since become unavailable; it MUST NOT fail solely because a reported root is now gone. Mirrors TS `shouldTolerateUnavailableRoot`. (R-21.1.5-j; AC-32.17) |

## Remarks

Roots are informational guidance, NOT an access-control boundary: the protocol does not enforce that a server confines itself to the listed roots (R-21.1.5-l). The constants/predicates here let server code assert it never relies on protocol-level enforcement.

## See Also

* namespace [Stackific.Mcp.Protocol](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
