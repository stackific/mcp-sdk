using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The discriminated union of content blocks carried by tool results and prompt messages
/// (spec §14.4). The wire discriminator is the <c>type</c> field; the SDK maps it to the
/// concrete subtype. The sampling-only <c>tool_use</c>/<c>tool_result</c> blocks (§21) are
/// deliberately excluded — they MUST NOT appear where a <see cref="ContentBlock"/> is expected (§14.8).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(ImageContent), "image")]
[JsonDerivedType(typeof(AudioContent), "audio")]
[JsonDerivedType(typeof(ResourceLink), "resource_link")]
[JsonDerivedType(typeof(EmbeddedResource), "resource")]
public abstract record ContentBlock
{
  private protected ContentBlock() { }

  /// <summary>OPTIONAL. Untrusted presentation hints for this block (§14.6).</summary>
  public Annotations? Annotations { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// Ergonomic factory helpers for building <see cref="ContentBlock"/> values (spec §14.4).
/// </summary>
public static class ContentBlocks
{
  /// <summary>Creates a <see cref="TextContent"/> block.</summary>
  /// <param name="text">The text.</param>
  /// <param name="annotations">Optional hints.</param>
  /// <returns>The block.</returns>
  public static TextContent Text(string text, Annotations? annotations = null) =>
    new() { Text = text, Annotations = annotations };

  /// <summary>Creates an <see cref="ImageContent"/> block from Base64 data.</summary>
  /// <param name="base64Data">Base64-encoded image bytes.</param>
  /// <param name="mimeType">The image MIME type (for example <c>image/png</c>).</param>
  /// <returns>The block.</returns>
  public static ImageContent Image(string base64Data, string mimeType) =>
    new() { Data = base64Data, MimeType = mimeType };

  /// <summary>Creates an <see cref="AudioContent"/> block from Base64 data.</summary>
  /// <param name="base64Data">Base64-encoded audio bytes.</param>
  /// <param name="mimeType">The audio MIME type (for example <c>audio/wav</c>).</param>
  /// <returns>The block.</returns>
  public static AudioContent Audio(string base64Data, string mimeType) =>
    new() { Data = base64Data, MimeType = mimeType };

  /// <summary>Creates an <see cref="EmbeddedResource"/> block carrying resource contents inline.</summary>
  /// <param name="resource">The embedded contents.</param>
  /// <returns>The block.</returns>
  public static EmbeddedResource Resource(ResourceContents resource) => new() { Resource = resource };

  /// <summary>Creates a <see cref="ResourceLink"/> block referencing a resource by URI.</summary>
  /// <param name="uri">The resource URI.</param>
  /// <param name="name">The programmatic resource name.</param>
  /// <param name="mimeType">The optional MIME type.</param>
  /// <param name="title">The optional display title.</param>
  /// <returns>The block.</returns>
  public static ResourceLink LinkTo(string uri, string name, string? mimeType = null, string? title = null) =>
    new() { Uri = uri, Name = name, MimeType = mimeType, Title = title };
}

/// <summary>Plain text content (spec §14.4.1).</summary>
public sealed record TextContent : ContentBlock
{
  /// <summary>REQUIRED. The text content.</summary>
  public required string Text { get; init; }
}

/// <summary>Base64-encoded image content with a MIME type (spec §14.4.2).</summary>
public sealed record ImageContent : ContentBlock
{
  /// <summary>REQUIRED. Base64-encoded image bytes.</summary>
  public required string Data { get; init; }

  /// <summary>REQUIRED. The image MIME type.</summary>
  public required string MimeType { get; init; }
}

/// <summary>Base64-encoded audio content with a MIME type (spec §14.4.3).</summary>
public sealed record AudioContent : ContentBlock
{
  /// <summary>REQUIRED. Base64-encoded audio bytes.</summary>
  public required string Data { get; init; }

  /// <summary>REQUIRED. The audio MIME type.</summary>
  public required string MimeType { get; init; }
}

/// <summary>A reference to a resource by URI rather than its contents (spec §14.4.4).</summary>
public sealed record ResourceLink : ContentBlock
{
  /// <summary>REQUIRED. The referenced resource URI.</summary>
  public required string Uri { get; init; }

  /// <summary>REQUIRED. The programmatic resource name (from <c>BaseMetadata</c>).</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. The human display name.</summary>
  public string? Title { get; init; }

  /// <summary>OPTIONAL. A description of the resource.</summary>
  public string? Description { get; init; }

  /// <summary>OPTIONAL. The resource MIME type, if known.</summary>
  public string? MimeType { get; init; }

  /// <summary>OPTIONAL. The raw resource size in bytes, if known.</summary>
  public long? Size { get; init; }

  /// <summary>OPTIONAL. Icons for display.</summary>
  public IReadOnlyList<Icon>? Icons { get; init; }
}

/// <summary>Resource contents embedded directly into a result or message (spec §14.4.5).</summary>
public sealed record EmbeddedResource : ContentBlock
{
  /// <summary>REQUIRED. The embedded contents (text or blob variant).</summary>
  public required ResourceContents Resource { get; init; }
}

/// <summary>
/// The concrete contents of a resource (spec §14.5). A value is the text variant if and only
/// if it carries <see cref="Text"/>, and the blob variant if and only if it carries
/// <see cref="Blob"/>; a value MUST NOT carry both. Use <see cref="OfText"/>/<see cref="OfBlob"/>
/// to construct a well-formed variant.
/// </summary>
public sealed record ResourceContents
{
  /// <summary>REQUIRED. The URI of the resource these contents belong to.</summary>
  public required string Uri { get; init; }

  /// <summary>OPTIONAL. The MIME type, if known.</summary>
  public string? MimeType { get; init; }

  /// <summary>The textual content (text variant).</summary>
  public string? Text { get; init; }

  /// <summary>Base64-encoded binary content (blob variant).</summary>
  public string? Blob { get; init; }

  /// <summary>OPTIONAL. Implementation-specific metadata (§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }

  /// <summary>Builds the text variant of resource contents.</summary>
  /// <param name="uri">The resource URI.</param>
  /// <param name="text">The textual content.</param>
  /// <param name="mimeType">The optional MIME type.</param>
  /// <returns>The contents.</returns>
  public static ResourceContents OfText(string uri, string text, string? mimeType = null) =>
    new() { Uri = uri, Text = text, MimeType = mimeType };

  /// <summary>Builds the blob variant of resource contents.</summary>
  /// <param name="uri">The resource URI.</param>
  /// <param name="base64Blob">Base64-encoded binary content.</param>
  /// <param name="mimeType">The optional MIME type.</param>
  /// <returns>The contents.</returns>
  public static ResourceContents OfBlob(string uri, string base64Blob, string? mimeType = null) =>
    new() { Uri = uri, Blob = base64Blob, MimeType = mimeType };
}
