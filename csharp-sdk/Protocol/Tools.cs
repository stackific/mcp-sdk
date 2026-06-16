using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// A tool definition (spec §16.3): a programmatic <see cref="Name"/>, a REQUIRED JSON Schema for
/// its arguments, and optional output schema, annotations, and display metadata.
/// </summary>
public sealed record Tool
{
  /// <summary>REQUIRED. The unique programmatic identifier used to invoke the tool.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name (precedence: <c>title</c> → <c>annotations.title</c> → <c>name</c>).</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. A human-readable description used as a model hint.</summary>
  public string? Description { get; init; }

  /// <summary>REQUIRED. JSON Schema (2020-12) for the arguments object; its root <c>type</c> MUST be <c>object</c> (§16.4).</summary>
  public required JsonObject InputSchema { get; init; }

  /// <summary>OPTIONAL. JSON Schema (2020-12) describing the result's <c>structuredContent</c> (§16.4).</summary>
  public JsonObject? OutputSchema { get; init; }

  /// <summary>OPTIONAL. Untrusted behavior hints (§16.7).</summary>
  public ToolAnnotations? Annotations { get; init; }

  /// <summary>OPTIONAL. Icons for display (§14.2).</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }

  /// <summary>OPTIONAL. Implementation- and extension-specific metadata (§4); carries the UI declaration when present (§26).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>The paginated, cacheable result of <c>tools/list</c> (spec §16.2).</summary>
public sealed record ListToolsResult
{
  /// <summary>REQUIRED. The page of tool definitions (may be empty).</summary>
  public required IReadOnlyList<Tool> Tools { get; init; }

  /// <summary>OPTIONAL. Opaque cursor for the next page; absent on the last page (§12).</summary>
  public string? NextCursor { get; init; }

  /// <summary>The cache time-to-live hint in milliseconds (§13).</summary>
  public long? TtlMs { get; init; }

  /// <summary>The cache sharing scope (§13).</summary>
  public CacheScope? CacheScope { get; init; }
}

/// <summary>
/// Untrusted, human- and model-oriented hints about a tool's behavior (spec §16.7). A client
/// MUST NOT make safety decisions based on annotations from an untrusted server.
/// </summary>
public sealed record ToolAnnotations
{
  /// <summary>OPTIONAL. A human-readable title (ranks after the tool's <c>title</c>, before <c>name</c>).</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL (default <c>false</c>). If <c>true</c>, the tool does not modify its environment.</summary>
  public bool? ReadOnlyHint { get; init; }

  /// <summary>OPTIONAL (default <c>true</c>). If <c>true</c>, the tool may perform destructive updates. Meaningful only when not read-only.</summary>
  public bool? DestructiveHint { get; init; }

  /// <summary>OPTIONAL (default <c>false</c>). If <c>true</c>, repeated calls with the same arguments have no extra effect. Meaningful only when not read-only.</summary>
  public bool? IdempotentHint { get; init; }

  /// <summary>OPTIONAL (default <c>true</c>). If <c>true</c>, the tool may interact with an open world of external entities.</summary>
  public bool? OpenWorldHint { get; init; }
}

/// <summary>
/// The result of a completed <c>tools/call</c> (spec §16.5): unstructured <see cref="Content"/>
/// blocks, optional <see cref="StructuredContent"/> (any JSON value), and an
/// <see cref="IsError"/> flag for <em>tool-execution</em> failures (§16.6 — distinct from a
/// JSON-RPC protocol error). The base <c>resultType</c> is supplied by the runtime.
/// </summary>
public sealed record CallToolResult
{
  /// <summary>REQUIRED. The unstructured result blocks (may be empty, may mix kinds).</summary>
  public required IReadOnlyList<ContentBlock> Content { get; init; }

  /// <summary>OPTIONAL. A structured result of any JSON type; populated when the tool declares an <c>outputSchema</c>.</summary>
  public JsonNode? StructuredContent { get; init; }

  /// <summary>OPTIONAL (absent ⇒ <c>false</c>). <c>true</c> when the tool ran but failed (§16.6).</summary>
  public bool? IsError { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4); may carry cache hints (§13).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }

  /// <summary>Builds a success result carrying a single text block.</summary>
  /// <param name="text">The text.</param>
  /// <returns>The result.</returns>
  public static CallToolResult FromText(string text) => new() { Content = [ContentBlocks.Text(text)] };

  /// <summary>Builds a tool-execution error result (<c>isError: true</c>) carrying a single text block (§16.6).</summary>
  /// <param name="text">The human- and model-readable explanation.</param>
  /// <returns>The result.</returns>
  public static CallToolResult FromError(string text) =>
    new() { Content = [ContentBlocks.Text(text)], IsError = true };
}
