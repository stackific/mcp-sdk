# Subscriptions class

Pure, side-effect-free helpers for server-to-client subscriptions (spec §10): the four change-notification kinds and the request-scoped kinds (§10.5, §10.6), absolute-URI validation, sub-resource (container) URI coverage with boundary safety (§10.5, R-10.5-j), the honored-subset computation including the `taskIds` gate (§10.3, §25.10), stream-boundary violation detection (§10.6), and the declined-kinds report (§10.3). The C# counterpart of the exported functions in the TypeScript `protocol/streaming.ts` (S16). The client-facing [`Subscription`](./Subscription.md) / [`SubscriptionRegistry`](./SubscriptionRegistry.md) lifecycle objects live alongside this class.

```csharp
public static class Subscriptions
```

## Public Members

| name | description |
| --- | --- |
| static readonly [ChangeNotificationMethods](Subscriptions/ChangeNotificationMethods.md) | The exactly-four change-notification kinds that flow on a subscription stream (§10.5, R-10.5-a). |
| static readonly [RequestScopedNotificationMethods](Subscriptions/RequestScopedNotificationMethods.md) | The two request-scoped kinds that travel on a request's own stream, never a subscription stream (§10.6, R-10.6-a). |
| static [ClassifyNotificationStream](Subscriptions/ClassifyNotificationStream.md)(…) | Classifies a notification *method* against the §10.6 boundary: a change kind → Subscription; `notifications/progress` / `notifications/message` → RequestScoped; anything else → Neither (R-10.6-c, R-10.6-a). |
| static [ComputeAcknowledgedFilter](Subscriptions/ComputeAcknowledgedFilter.md)(…) | Computes the honored-subset filter for the acknowledgement: a kind is honored only when the client requested it AND the gating server capability/sub-flag is declared; unsupported kinds are OMITTED (§10.3, R-10.3-c, R-10.3-d). For `resourceSubscriptions`, the honored list is the requested URIs when `resources.subscribe` is declared. For `taskIds`, the honored list is the requested ids ONLY when *tasksActive* (the Tasks extension is active for this request, §25.10). |
| static [DeclinedFilterKinds](Subscriptions/DeclinedFilterKinds.md)(…) | Returns the kinds the client requested but the server did NOT honor (declined), so a client can handle them gracefully and not block on a declined kind (§10.3, R-10.3-f). |
| static [IsAbsoluteUri](Subscriptions/IsAbsoluteUri.md)(…) | Returns `true` when *value* is an absolute URI string [RFC3986] — a scheme followed by `:` and at least one further character (§10.2, R-10.2-i). A relative reference (no scheme) is rejected. |
| static [IsChangeNotificationMethod](Subscriptions/IsChangeNotificationMethod.md)(…) | Returns `true` when *method* is one of the four subscription change kinds (R-10.5-a). |
| static [IsEmptySubscriptionFilter](Subscriptions/IsEmptySubscriptionFilter.md)(…) | Returns `true` when the filter requests no kinds at all — every boolean is absent/`false` and both `resourceSubscriptions` and `taskIds` are absent/empty (§10.2, R-10.2-k). Such a filter yields an acknowledgement-only stream. |
| static [IsRequestScopedNotificationMethod](Subscriptions/IsRequestScopedNotificationMethod.md)(…) | Returns `true` when *method* is a request-scoped (progress/logging) kind (R-10.6-a). |
| static [IsViolationOnRequestStream](Subscriptions/IsViolationOnRequestStream.md)(…) | Returns `true` when receiving notification *method* on an unrelated request's response stream is a protocol violation — i.e. it is one of the four change kinds, which MUST NOT appear on a non-`subscriptions/listen` response stream (§10.6, R-10.6-d, R-10.6-f, R-10.6-g). |
| static [IsViolationOnSubscriptionStream](Subscriptions/IsViolationOnSubscriptionStream.md)(…) | Returns `true` when receiving notification *method* on a SUBSCRIPTION stream is a protocol violation — i.e. it is a request-scoped (progress/logging) kind, which MUST NOT appear there (§10.6, R-10.6-b, R-10.6-e, R-10.6-g). |
| static [MayDeliverResourceUpdate](Subscriptions/MayDeliverResourceUpdate.md)(…) | Returns `true` when a `notifications/resources/updated` for *updatedUri* is permitted on a subscription whose acknowledged `resourceSubscriptions` are *subscribedUris* — i.e. the URI (or a parent container) was listed (§10.2, §10.5, R-10.2-l, R-10.5-h). A server MUST NOT send an update for an unlisted resource. |
| static [MayEmitChangeNotification](Subscriptions/MayEmitChangeNotification.md)(…) | Returns `true` when the server MAY emit the change notification *method* on a subscription stream whose acknowledged filter is *acknowledged* (§10.5, R-10.5-l). For `notifications/resources/updated`, pass the updated URI as *subjectKey*; for `notifications/tasks`, pass the task id (§25.10). A kind is emittable only when its filter field is reflected in the acknowledged filter. |
| static [UriCoveredBySubscription](Subscriptions/UriCoveredBySubscription.md)(…) | Returns `true` when *updatedUri* is covered by *subscribedUri* — either an exact match or a sub-resource of a subscribed CONTAINER URI (the updated URI MAY be a descendant) (§10.5, R-10.5-j). Container matching is path-prefix based after a normalized scheme+host compare: `file:///dir` covers `file:///dir/file.txt`. A bare prefix that is not a path boundary (`file:///dir` vs `file:///directory`) does NOT match. |

## See Also

* namespace [Stackific.Mcp.Protocol](../README.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
