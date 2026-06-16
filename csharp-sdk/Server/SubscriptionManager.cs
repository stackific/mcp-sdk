using System.Collections.Concurrent;
using System.Text.Json.Nodes;

using Stackific.Mcp.Json;
using Stackific.Mcp.JsonRpc;
using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Server;

/// <summary>
/// Tracks active <c>subscriptions/listen</c> streams and fans server-initiated change notifications out
/// to the ones that opted into each kind (spec §10.5). Each delivered notification is tagged with the
/// subscription identifier in <c>_meta</c> (§10.4), and the server only honors the filter kinds it
/// actually supports (§10.3).
/// </summary>
public sealed class SubscriptionManager
{
  private readonly ConcurrentDictionary<string, Entry> _subscriptions = new(StringComparer.Ordinal);

  /// <summary>
  /// Registers a subscription stream and returns the filter the server agreed to honor plus a teardown
  /// handle (spec §10.3). The honored filter is the intersection of the requested kinds and the kinds
  /// the server's capabilities support.
  /// </summary>
  /// <param name="requested">The filter the client requested.</param>
  /// <param name="capabilities">The server's advertised capabilities.</param>
  /// <param name="subscriptionId">The subscription id (the <c>subscriptions/listen</c> request id, as a string).</param>
  /// <param name="deliver">The sink that writes a notification onto this subscription's stream.</param>
  /// <returns>The honored filter and a disposable that unregisters the subscription.</returns>
  public (SubscriptionFilter Honored, IDisposable Teardown) Register(
    SubscriptionFilter requested,
    ServerCapabilities capabilities,
    string subscriptionId,
    Func<JsonRpcNotification, Task> deliver)
  {
    var honored = Honor(requested, capabilities);
    _subscriptions[subscriptionId] = new Entry(honored, deliver);
    return (honored, new Teardown(this, subscriptionId));
  }

  /// <summary>Fans a change notification out to every subscription whose honored filter selects its kind (spec §10.5).</summary>
  /// <param name="notification">The change notification (a list-changed or resource-updated kind).</param>
  /// <returns>A task that completes when delivery to all matching subscriptions is done.</returns>
  public async Task FanOutAsync(JsonRpcNotification notification)
  {
    foreach (var (id, entry) in _subscriptions)
    {
      if (!Matches(entry.Filter, notification)) continue;
      await entry.Deliver(Tag(notification, id)).ConfigureAwait(false);
    }
  }

  private static SubscriptionFilter Honor(SubscriptionFilter requested, ServerCapabilities capabilities) => new()
  {
    ToolsListChanged = requested.ToolsListChanged == true && capabilities.Tools?.ListChanged == true ? true : null,
    PromptsListChanged = requested.PromptsListChanged == true && capabilities.Prompts?.ListChanged == true ? true : null,
    ResourcesListChanged = requested.ResourcesListChanged == true && capabilities.Resources?.ListChanged == true ? true : null,
    ResourceSubscriptions = capabilities.Resources?.Subscribe == true ? requested.ResourceSubscriptions : null,
  };

  private static bool Matches(SubscriptionFilter filter, JsonRpcNotification notification) => notification.Method switch
  {
    McpMethods.NotificationsToolsListChanged => filter.ToolsListChanged == true,
    McpMethods.NotificationsPromptsListChanged => filter.PromptsListChanged == true,
    McpMethods.NotificationsResourcesListChanged => filter.ResourcesListChanged == true,
    McpMethods.NotificationsResourcesUpdated => filter.ResourceSubscriptions is { } uris
      && notification.Params?["uri"]?.GetValue<string>() is { } uri && uris.Contains(uri),
    _ => false,
  };

  private static JsonRpcNotification Tag(JsonRpcNotification notification, string subscriptionId)
  {
    var prms = notification.Params is null ? new JsonObject() : (JsonObject)notification.Params.DeepClone();
    var meta = prms["_meta"] as JsonObject ?? new JsonObject();
    meta[MetaKeys.SubscriptionId] = subscriptionId;
    prms["_meta"] = meta;
    return notification with { Params = prms };
  }

  private sealed record Entry(SubscriptionFilter Filter, Func<JsonRpcNotification, Task> Deliver);

  private sealed class Teardown(SubscriptionManager manager, string subscriptionId) : IDisposable
  {
    public void Dispose() => manager._subscriptions.TryRemove(subscriptionId, out _);
  }
}
