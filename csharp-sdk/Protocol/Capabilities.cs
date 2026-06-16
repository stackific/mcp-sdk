using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The capabilities a client advertises, per request, in the
/// <c>io.modelcontextprotocol/clientCapabilities</c> metadata key (spec §6.2). A capability
/// is declared by the <em>presence</em> of its field — an empty object <c>{}</c> still means
/// "supported"; absence (here, <c>null</c>) means "not supported". Unknown fields are ignored
/// on receipt (§6.6).
/// </summary>
public sealed record ClientCapabilities
{
  /// <summary>A shared, empty capability set declaring no optional client behaviors.</summary>
  public static ClientCapabilities None { get; } = new();

  /// <summary>Non-standard, experimental capabilities keyed by identifier (§6.2).</summary>
  public IDictionary<string, JsonObject>? Experimental { get; init; }

  /// <summary>Present if the client supports server-initiated elicitation (§20).</summary>
  public ElicitationCapability? Elicitation { get; init; }

  /// <summary>Present if the client exposes filesystem roots. Status: <b>Deprecated</b> (§21).</summary>
  public JsonObject? Roots { get; init; }

  /// <summary>Present if the client supports server-initiated sampling. Status: <b>Deprecated</b> (§21).</summary>
  public SamplingCapability? Sampling { get; init; }

  /// <summary>The MCP extensions the client supports, keyed by extension identifier (§6.5).</summary>
  public IDictionary<string, JsonObject>? Extensions { get; init; }

  /// <summary><c>true</c> if the client declared elicitation support (form mode is the implicit baseline, §6.2).</summary>
  [JsonIgnore]
  public bool SupportsElicitation => Elicitation is not null;

  /// <summary><c>true</c> if the client supports URL-mode elicitation (§6.2).</summary>
  [JsonIgnore]
  public bool SupportsElicitationUrl => Elicitation?.Url is not null;

  /// <summary><c>true</c> if the client declared the deprecated sampling capability (§21).</summary>
  [JsonIgnore]
  public bool SupportsSampling => Sampling is not null;

  /// <summary><c>true</c> if the client declared the deprecated roots capability (§21).</summary>
  [JsonIgnore]
  public bool SupportsRoots => Roots is not null;

  /// <summary>Returns <c>true</c> if the client advertised the extension <paramref name="identifier"/> (§6.5).</summary>
  /// <param name="identifier">The extension identifier, for example <c>io.modelcontextprotocol/tasks</c>.</param>
  /// <returns><c>true</c> when advertised.</returns>
  public bool HasExtension(string identifier) => Extensions is not null && Extensions.ContainsKey(identifier);
}

/// <summary>Client elicitation capability with its mode sub-flags (spec §6.2/§20).</summary>
public sealed record ElicitationCapability
{
  /// <summary>Present if the client supports form-mode elicitation (the baseline mode).</summary>
  public JsonObject? Form { get; init; }

  /// <summary>Present if the client supports URL-mode elicitation.</summary>
  public JsonObject? Url { get; init; }
}

/// <summary>Deprecated client sampling capability with its sub-flags (spec §6.2/§21).</summary>
public sealed record SamplingCapability
{
  /// <summary>Present if the client supports sampling context inclusion (deprecated).</summary>
  public JsonObject? Context { get; init; }

  /// <summary>Present if the client supports tool use within sampling.</summary>
  public JsonObject? Tools { get; init; }
}

/// <summary>
/// The capabilities a server advertises in its <c>server/discover</c> result (spec §6.3).
/// As with client capabilities, presence declares support and unknown fields are ignored (§6.6).
/// </summary>
public sealed record ServerCapabilities
{
  /// <summary>Non-standard, experimental capabilities keyed by identifier (§6.3).</summary>
  public IDictionary<string, JsonObject>? Experimental { get; init; }

  /// <summary>Present if the server emits log messages. Status: <b>Deprecated</b> (§15.3).</summary>
  public JsonObject? Logging { get; init; }

  /// <summary>Present if the server supports argument completion via <c>completion/complete</c> (§19).</summary>
  public JsonObject? Completions { get; init; }

  /// <summary>Present if the server offers prompts (§18).</summary>
  public PromptsCapability? Prompts { get; init; }

  /// <summary>Present if the server offers resources (§17).</summary>
  public ResourcesCapability? Resources { get; init; }

  /// <summary>Present if the server offers tools (§16).</summary>
  public ToolsCapability? Tools { get; init; }

  /// <summary>The MCP extensions the server supports, keyed by extension identifier (§6.5).</summary>
  public IDictionary<string, JsonObject>? Extensions { get; init; }

  /// <summary>Returns <c>true</c> if the server advertised the extension <paramref name="identifier"/> (§6.5).</summary>
  /// <param name="identifier">The extension identifier.</param>
  /// <returns><c>true</c> when advertised.</returns>
  public bool HasExtension(string identifier) => Extensions is not null && Extensions.ContainsKey(identifier);
}

/// <summary>Server prompts capability (spec §6.3/§18).</summary>
public sealed record PromptsCapability
{
  /// <summary>When <c>true</c>, the server emits <c>notifications/prompts/list_changed</c> (§18.6).</summary>
  public bool? ListChanged { get; init; }
}

/// <summary>Server resources capability with its sub-flags (spec §6.3/§17).</summary>
public sealed record ResourcesCapability
{
  /// <summary>When <c>true</c>, the server supports per-resource update subscriptions (§10/§17.7).</summary>
  public bool? Subscribe { get; init; }

  /// <summary>When <c>true</c>, the server emits <c>notifications/resources/list_changed</c> (§17.7).</summary>
  public bool? ListChanged { get; init; }
}

/// <summary>Server tools capability (spec §6.3/§16).</summary>
public sealed record ToolsCapability
{
  /// <summary>When <c>true</c>, the server emits <c>notifications/tools/list_changed</c> (§16.8).</summary>
  public bool? ListChanged { get; init; }
}
