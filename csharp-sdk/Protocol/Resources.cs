using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// A concrete, directly readable resource identified by a URI (spec §17.4). Composes the
/// <c>BaseMetadata</c> name/title pair and the optional icons array.
/// </summary>
public sealed record Resource
{
  /// <summary>REQUIRED. The URI uniquely identifying this resource (any scheme).</summary>
  public required string Uri { get; init; }

  /// <summary>REQUIRED. The programmatic resource name.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name (preferred over <see cref="Name"/> for display).</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. Prose describing what the resource represents.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL. The MIME type of the resource content, if known.</summary>
  public string? MimeType { get; init; }

  /// <summary>OPTIONAL. The raw content size in bytes (before encoding/tokenization), if known.</summary>
  public long? Size { get; init; }

  /// <summary>OPTIONAL. Untrusted presentation hints (§14.6).</summary>
  public Annotations? Annotations { get; init; }

  /// <summary>OPTIONAL. Icons for display (§14.2).</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// A family of resources whose concrete URIs are produced by expanding an RFC 6570 URI Template
/// (spec §17.4). Has no <c>size</c> (size is a property of a concrete resource).
/// </summary>
public sealed record ResourceTemplate
{
  /// <summary>REQUIRED. An RFC 6570 URI Template (for example <c>weather://{city}/current</c>).</summary>
  public required string UriTemplate { get; init; }

  /// <summary>REQUIRED. The programmatic template name.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name.</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. Prose describing the template's purpose.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL. A MIME type shared by every resource matching this template, if uniform.</summary>
  public string? MimeType { get; init; }

  /// <summary>OPTIONAL. Untrusted presentation hints (§14.6).</summary>
  public Annotations? Annotations { get; init; }

  /// <summary>OPTIONAL. Icons for display (§14.2).</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>The paginated, cacheable result of <c>resources/list</c> (spec §17.2).</summary>
public sealed record ListResourcesResult
{
  /// <summary>REQUIRED. The page of resources (may be empty).</summary>
  public required IReadOnlyList<Resource> Resources { get; init; }

  /// <summary>OPTIONAL. Opaque cursor for the next page; absent on the last page (§12).</summary>
  public string? NextCursor { get; init; }

  /// <summary>The cache time-to-live hint in milliseconds (§13).</summary>
  public long? TtlMs { get; init; }

  /// <summary>The cache sharing scope (§13).</summary>
  public CacheScope? CacheScope { get; init; }
}

/// <summary>The paginated, cacheable result of <c>resources/templates/list</c> (spec §17.3).</summary>
public sealed record ListResourceTemplatesResult
{
  /// <summary>REQUIRED. The page of resource templates (may be empty).</summary>
  public required IReadOnlyList<ResourceTemplate> ResourceTemplates { get; init; }

  /// <summary>OPTIONAL. Opaque cursor for the next page; absent on the last page (§12).</summary>
  public string? NextCursor { get; init; }

  /// <summary>The cache time-to-live hint in milliseconds (§13).</summary>
  public long? TtlMs { get; init; }

  /// <summary>The cache sharing scope (§13).</summary>
  public CacheScope? CacheScope { get; init; }
}

/// <summary>The cacheable result of <c>resources/read</c> (spec §17.5).</summary>
public sealed record ReadResourceResult
{
  /// <summary>REQUIRED. One or more content entries (text or blob variant); never empty for an existing resource.</summary>
  public required IReadOnlyList<ResourceContents> Contents { get; init; }

  /// <summary>The cache time-to-live hint in milliseconds (§13).</summary>
  public long? TtlMs { get; init; }

  /// <summary>The cache sharing scope (§13).</summary>
  public CacheScope? CacheScope { get; init; }
}

/// <summary>Parameters of the <c>notifications/resources/updated</c> notification (spec §17.7).</summary>
public sealed record ResourceUpdatedNotificationParams
{
  /// <summary>REQUIRED. The URI of the resource that changed (MAY be a sub-resource of the subscribed URI).</summary>
  public required string Uri { get; init; }
}
