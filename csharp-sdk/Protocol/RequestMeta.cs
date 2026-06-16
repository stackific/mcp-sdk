using System.Text.Json;
using System.Text.Json.Nodes;

using Stackific.Mcp.Json;
using Stackific.Mcp.JsonRpc;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The protocol-defined per-request <c>_meta</c> envelope (spec §4.3) that makes every client
/// request self-describing: the protocol revision, the client identity, and the client's
/// per-request capabilities, plus optional passthrough keys (progress token, trace context,
/// third-party metadata). This is what enables the stateless model of §4.4.
/// </summary>
public sealed record RequestMeta
{
  /// <summary>REQUIRED. The protocol revision this request declares (§5.2).</summary>
  public required string ProtocolVersion { get; init; }

  /// <summary>REQUIRED. The client software identity (§4.3).</summary>
  public required Implementation ClientInfo { get; init; }

  /// <summary>REQUIRED. The client's capabilities for this request (§4.3); empty means none.</summary>
  public required ClientCapabilities ClientCapabilities { get; init; }

  /// <summary>OPTIONAL, Deprecated. The minimum log severity the server may emit for this request (§4.3/§15.3).</summary>
  public string? LogLevel { get; init; }

  /// <summary>
  /// Any additional <c>_meta</c> keys beyond the protocol-defined ones — for example
  /// <c>progressToken</c>, <c>traceparent</c>/<c>tracestate</c>/<c>baggage</c>, or third-party
  /// keys. Carried through unchanged so receivers can echo or act on them (§4.2).
  /// </summary>
  public JsonObject? Additional { get; init; }

  /// <summary>Builds the wire <c>_meta</c> object carrying the protocol-defined keys plus any <see cref="Additional"/>.</summary>
  /// <returns>A fresh <see cref="JsonObject"/> suitable for placing on request <c>params._meta</c>.</returns>
  public JsonObject ToJsonObject()
  {
    var meta = new JsonObject();
    if (Additional is not null)
    {
      foreach (var (key, value) in Additional)
      {
        meta[key] = value?.DeepClone();
      }
    }
    meta[MetaKeys.ProtocolVersion] = ProtocolVersion;
    meta[MetaKeys.ClientInfo] = JsonSerializer.SerializeToNode(ClientInfo, McpJson.Options);
    meta[MetaKeys.ClientCapabilities] = JsonSerializer.SerializeToNode(ClientCapabilities, McpJson.Options);
    if (LogLevel is not null) meta[MetaKeys.LogLevel] = LogLevel;
    return meta;
  }

  /// <summary>
  /// Parses and validates the per-request <c>_meta</c> envelope from request <c>params</c>
  /// (server side). A request that omits any REQUIRED key is malformed and is rejected with
  /// <c>-32602</c> (Invalid params) per §4.3.
  /// </summary>
  /// <param name="paramsObject">The request's <c>params</c> object (may be <c>null</c>).</param>
  /// <returns>The parsed envelope.</returns>
  /// <exception cref="McpError">-32602 when <c>_meta</c> or any required key is missing or malformed.</exception>
  public static RequestMeta Parse(JsonObject? paramsObject)
  {
    if (paramsObject is null || paramsObject["_meta"] is not JsonObject meta)
    {
      throw McpError.InvalidParams("Request params must carry a \"_meta\" object with the required per-request keys (§4.3).");
    }

    var protocolVersion = RequireString(meta, MetaKeys.ProtocolVersion);
    var clientInfo = RequireObject<Implementation>(meta, MetaKeys.ClientInfo);
    var clientCapabilities = RequireObject<ClientCapabilities>(meta, MetaKeys.ClientCapabilities);

    string? logLevel = null;
    if (meta[MetaKeys.LogLevel] is JsonValue logValue && logValue.GetValueKind() == JsonValueKind.String)
    {
      logLevel = logValue.GetValue<string>();
    }

    // Preserve every other key (progressToken, trace context, third-party) verbatim.
    var additional = new JsonObject();
    foreach (var (key, value) in meta)
    {
      if (key is MetaKeys.ProtocolVersion or MetaKeys.ClientInfo or MetaKeys.ClientCapabilities or MetaKeys.LogLevel)
      {
        continue;
      }
      additional[key] = value?.DeepClone();
    }

    return new RequestMeta
    {
      ProtocolVersion = protocolVersion,
      ClientInfo = clientInfo,
      ClientCapabilities = clientCapabilities,
      LogLevel = logLevel,
      Additional = additional.Count > 0 ? additional : null,
    };
  }

  private static string RequireString(JsonObject meta, string key)
  {
    if (meta[key] is JsonValue value && value.GetValueKind() == JsonValueKind.String)
    {
      return value.GetValue<string>();
    }
    throw McpError.InvalidParams($"Required request metadata key \"{key}\" is missing or not a string (§4.3).");
  }

  private static T RequireObject<T>(JsonObject meta, string key)
  {
    if (meta[key] is not JsonObject node)
    {
      throw McpError.InvalidParams($"Required request metadata key \"{key}\" is missing or not an object (§4.3).");
    }
    try
    {
      return node.Deserialize<T>(McpJson.Options)
        ?? throw McpError.InvalidParams($"Request metadata key \"{key}\" could not be read (§4.3).");
    }
    catch (JsonException error)
    {
      throw McpError.InvalidParams($"Request metadata key \"{key}\" is malformed: {error.Message}");
    }
  }
}
