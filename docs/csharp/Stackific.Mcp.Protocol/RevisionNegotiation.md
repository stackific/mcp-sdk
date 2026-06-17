# RevisionNegotiation class

Revision selection and the recovery paths for the two negotiation errors (spec §5.4–§5.7) — the C# counterpart of the TypeScript `protocol/negotiation.ts` module. It turns the raw materials discovery produces (a set of advertised revisions and capabilities) into a chosen protocol revision, and defines what a client does when a request is rejected.

```csharp
public static class RevisionNegotiation
```

## Public Members

| name | description |
| --- | --- |
| const [NegotiationErrorHttpStatus](RevisionNegotiation/NegotiationErrorHttpStatus.md) | Both negotiation errors ride HTTP `400 Bad Request` on the HTTP transport (R-5.5-b, R-5.6-d). |
| const [ServerDiscoverMethod](RevisionNegotiation/ServerDiscoverMethod.md) | The method a client sends as its opening probe request (R-5.7-b): `server/discover`. |
| static [AugmentClientCapabilities](RevisionNegotiation/AugmentClientCapabilities.md)(…) | Produces the `clientCapabilities` object for a retry after a `MissingRequiredClientCapability` (`-32003`) error: the originally declared capabilities merged with the required ones (§5.6, R-5.6-i). The merge is shallow — a required capability replaces any previously declared value for that key — and never mutates its inputs. Mirrors TS `augmentClientCapabilities`. |
| static [CanSatisfyRequiredCapabilities](RevisionNegotiation/CanSatisfyRequiredCapabilities.md)(…) | Returns `true` when the client can declare every capability the server named as required — each required top-level key is one the client already declares (§5.6, R-5.6-i). The comparison is by top-level key presence; capabilities are never inferred from a prior request. Mirrors TS `canSatisfyRequiredCapabilities`. |
| static [DeterminationFromProbe](RevisionNegotiation/DeterminationFromProbe.md)(…) | Derives a [`ProtocolSupportDetermination`](./ProtocolSupportDetermination.md) from a probe outcome, ready to cache (§5.7). Both Supported and UnsupportedVersion mean the server speaks this protocol family; NotThisProtocol means it does not (R-5.7-c). Mirrors TS `determinationFromProbe`. |
| static [HttpStatusForNegotiationError](RevisionNegotiation/HttpStatusForNegotiationError.md)(…) | Returns `400` when *code* is one of the two negotiation error codes (`-32004`, `-32003`), which on the HTTP transport MUST ride a `400 Bad Request`; otherwise `null` (R-5.5-b, R-5.6-d). Mirrors TS `httpStatusForNegotiationError`. |
| static [InterpretProbeResponse](RevisionNegotiation/InterpretProbeResponse.md)(…) | Interprets a response to a probe `server/discover` request (§5.7), classifying it as Supported, UnsupportedVersion, or NotThisProtocol. Mirrors TS `interpretProbeResponse`. |
| static [NameSupportedRevisionsInError](RevisionNegotiation/NameSupportedRevisionsInError.md)(…) | Adds the server's supported revisions to an error's `data.supported` so a peer with no fall-forward mechanism can still surface a useful diagnostic (§5.7, R-5.7-g). Existing `data` fields are preserved; `supported` is set/overwritten. Never mutates the input. Mirrors TS `nameSupportedRevisionsInError`. |
| static [NegotiateRevision](RevisionNegotiation/NegotiateRevision.md)(…) | Selects the highest mutually supported protocol revision, returning a structured outcome (§5.4, R-5.4-b). On an empty intersection the result carries both sides' revision sets so the caller can surface an [`IncompatibleProtocolError`](./IncompatibleProtocolError.md) (R-5.4-c, R-5.4-d). Mirrors TS `negotiateRevision`. |
| static [ReselectAfterUnsupportedVersion](RevisionNegotiation/ReselectAfterUnsupportedVersion.md)(…) | Reacts to an `UnsupportedProtocolVersion` (`-32004`) error by re-selecting a revision from the error's authoritative `data.supported` set (§5.5, R-5.5-h). Because this is a pure re-selection over the server's set, an empty result is terminal — the client MUST NOT retry indefinitely (R-5.5-i) and SHOULD surface an incompatibility (R-5.5-j). Mirrors TS `reselectAfterUnsupportedVersion`. |
| static [SelectRevision](RevisionNegotiation/SelectRevision.md)(…) | Selects a protocol revision from a server's *supportedVersions* using the client's own preference order (§5.4, R-5.3.2-d) — never the order of the server's array. The first client-preferred revision the server also supports is chosen; reordering *supportedVersions* cannot change the result. Returns `null` when the two share no revision. Mirrors TS `selectRevision`. |

## Remarks

The selection rule (§5.4) picks the highest mutually supported revision — the first in the client's own ordered preference list that also appears in the server's set — using exact string match, never lexical or chronological comparison (R-5.1-a, R-5.1-b). When the intersection is empty the client MUST NOT fabricate a revision (R-5.4-c) and SHOULD surface an [`IncompatibleProtocolError`](./IncompatibleProtocolError.md) (R-5.4-d). The §5.7 backward-compatibility probe and the per-endpoint [`ProtocolSupportCache`](./ProtocolSupportCache.md) live alongside it.

`server/discover` is OPTIONAL before a first substantive request (R-5.4-a): a client MAY probe first, or proceed directly and handle an `UnsupportedProtocolVersion` rejection — the re-selection path ([`ReselectAfterUnsupportedVersion`](./RevisionNegotiation/ReselectAfterUnsupportedVersion.md)) works without any prior discovery.

## See Also

* namespace [Stackific.Mcp.Protocol](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
