using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// A prompt or prompt template offered by the server (spec §18.3): a named, optionally
/// argument-accepting template that renders into conversation messages via <c>prompts/get</c>.
/// </summary>
public sealed record Prompt
{
  /// <summary>REQUIRED. The programmatic prompt identifier supplied in <c>prompts/get</c>.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name (preferred over <see cref="Name"/> for display).</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. A human-readable description of what the prompt provides.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL. The arguments the prompt accepts for templating (absent/empty ⇒ none).</summary>
  public IReadOnlyList<PromptArgument>? Arguments { get; init; }

  /// <summary>OPTIONAL. Icons for display (§14.2).</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>A single argument a prompt accepts (spec §18.3).</summary>
public sealed record PromptArgument
{
  /// <summary>REQUIRED. The argument's programmatic name (the key in the <c>prompts/get</c> arguments map).</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name.</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. A human-readable description.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL (default <c>false</c>). When <c>true</c>, the argument MUST be supplied (else <c>-32602</c>).</summary>
  public bool? Required { get; init; }
}

/// <summary>One message within a resolved prompt (spec §18.5): a role paired with a single content block.</summary>
public sealed record PromptMessage
{
  /// <summary>REQUIRED. The speaker of the message.</summary>
  public required Role Role { get; init; }

  /// <summary>REQUIRED. Exactly one content block (a single object, not an array).</summary>
  public required ContentBlock Content { get; init; }
}

/// <summary>The paginated, cacheable result of <c>prompts/list</c> (spec §18.2).</summary>
public sealed record ListPromptsResult
{
  /// <summary>REQUIRED. The page of prompts (may be empty).</summary>
  public required IReadOnlyList<Prompt> Prompts { get; init; }

  /// <summary>OPTIONAL. Opaque cursor for the next page; absent on the last page (§12).</summary>
  public string? NextCursor { get; init; }

  /// <summary>The cache time-to-live hint in milliseconds (§13).</summary>
  public long? TtlMs { get; init; }

  /// <summary>The cache sharing scope (§13).</summary>
  public CacheScope? CacheScope { get; init; }
}

/// <summary>The result of a completed <c>prompts/get</c> (spec §18.4).</summary>
public sealed record GetPromptResult
{
  /// <summary>OPTIONAL. A human-readable description of the rendered prompt.</summary>
  public string? Description { get; init; }

  /// <summary>REQUIRED. The ordered messages constituting the prompt.</summary>
  public required IReadOnlyList<PromptMessage> Messages { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}
