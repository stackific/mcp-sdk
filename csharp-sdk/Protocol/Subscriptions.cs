using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The notification kinds a client opts in to on a <c>subscriptions/listen</c> stream (spec §10.2).
/// All fields are OPTIONAL; an omitted/false field means "not subscribed". The server echoes the
/// subset it will honor in the acknowledgement (§10.3).
/// </summary>
public sealed record SubscriptionFilter
{
  /// <summary>When <c>true</c>, deliver <c>notifications/tools/list_changed</c> on this stream (§16.8).</summary>
  public bool? ToolsListChanged { get; init; }

  /// <summary>When <c>true</c>, deliver <c>notifications/prompts/list_changed</c> on this stream (§18.6).</summary>
  public bool? PromptsListChanged { get; init; }

  /// <summary>When <c>true</c>, deliver <c>notifications/resources/list_changed</c> on this stream (§17.7).</summary>
  public bool? ResourcesListChanged { get; init; }

  /// <summary>The absolute resource URIs to watch for <c>notifications/resources/updated</c> (§17.7); absent/empty ⇒ none.</summary>
  public IReadOnlyList<string>? ResourceSubscriptions { get; init; }
}

/// <summary>Parameters of the <c>subscriptions/listen</c> request (spec §10.2).</summary>
public sealed record SubscriptionsListenRequestParams
{
  /// <summary>REQUIRED. The notification kinds the client opts in to on this stream.</summary>
  public required SubscriptionFilter Notifications { get; init; }
}

/// <summary>
/// Parameters of the <c>notifications/subscriptions/acknowledged</c> notification (spec §10.3) — the
/// first message on every subscription stream. The subscription identifier is carried in the
/// notification's <c>_meta</c> under <c>io.modelcontextprotocol/subscriptionId</c> (§10.4).
/// </summary>
public sealed record SubscriptionsAcknowledgedNotificationParams
{
  /// <summary>REQUIRED. The subset of the requested filter the server agreed to honor (§10.3).</summary>
  public required SubscriptionFilter Notifications { get; init; }
}
