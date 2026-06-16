using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// Identifies a client or server software implementation (spec §14.3). Carried as
/// <c>clientInfo</c> in every request's <c>_meta</c> (§4.3) and as <c>serverInfo</c> in the
/// discovery result (§5.3.2). <see cref="Name"/> and <see cref="Version"/> are REQUIRED.
/// </summary>
public sealed record Implementation
{
  /// <summary>REQUIRED. Programmatic identifier of the implementation.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. Human-readable display name.</summary>
  public string? Title { get; init; }

  /// <summary>REQUIRED. Implementation version string.</summary>
  public required string Version { get; init; }

  /// <summary>OPTIONAL. Human-readable description of the implementation's purpose.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL. URI of the implementation's website.</summary>
  public string? WebsiteUrl { get; init; }

  /// <summary>OPTIONAL. Visual identifiers for the implementation (§14.2).</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }
}

/// <summary>
/// A single icon descriptor (spec §14.2): a source URI with optional MIME type and sizes.
/// </summary>
public sealed record Icon
{
  /// <summary>REQUIRED. The icon source — an absolute URL or a <c>data:</c> URI.</summary>
  public required string Src { get; init; }

  /// <summary>OPTIONAL. The icon's MIME type (for example <c>image/png</c>).</summary>
  public string? MimeType { get; init; }

  /// <summary>OPTIONAL. One or more space-separated <c>WxH</c> sizes (HTML <c>sizes</c> syntax), or <c>any</c>.</summary>
  public string? Sizes { get; init; }
}
