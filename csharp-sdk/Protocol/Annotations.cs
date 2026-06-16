using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The author/recipient role of a message or the intended audience of content (spec §14.7).
/// The only permitted wire values are <c>user</c> and <c>assistant</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Role>))]
public enum Role
{
  /// <summary>The human participant.</summary>
  [JsonStringEnumMemberName("user")]
  User,

  /// <summary>The language-model participant.</summary>
  [JsonStringEnumMemberName("assistant")]
  Assistant,
}

/// <summary>
/// Optional, untrusted hints about a piece of content or a resource (spec §14.6). A consumer
/// MUST NOT rely on annotation values for security or correctness decisions.
/// </summary>
public sealed record Annotations
{
  /// <summary>OPTIONAL. The intended audience(s) for the annotated object.</summary>
  public IReadOnlyList<Role>? Audience { get; init; }

  /// <summary>OPTIONAL. Importance from 0 (least) to 1 (most), inclusive.</summary>
  public double? Priority { get; init; }

  /// <summary>OPTIONAL. ISO-8601 timestamp of the last modification.</summary>
  public string? LastModified { get; init; }
}
