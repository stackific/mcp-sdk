using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// A filesystem "root" — a directory or file the client considers relevant — exposed to a server
/// as informational guidance (spec §21.1.5).
/// </summary>
/// <remarks>
/// This type belongs to the <b>Deprecated</b> Roots capability (spec §21.1). Implementations SHOULD
/// NOT adopt it for new functionality; it remains defined for interoperability. Prefer conveying
/// relevant directories and files through tool input parameters (§16), resource URIs (§17), or
/// server configuration. Roots are <em>not</em> an access-control mechanism: the protocol does not
/// enforce that a server confines its operations to the listed roots.
/// </remarks>
public sealed record Root
{
  /// <summary>
  /// REQUIRED. The URI identifying the root (spec §21.1.5). In this revision it MUST use the
  /// <c>file</c> scheme — that is, it MUST begin with <c>file://</c> — and MUST be a syntactically
  /// valid URI [RFC3986]. A receiver MAY reject or ignore a root whose URI does not use the
  /// <c>file</c> scheme.
  /// </summary>
  public required string Uri { get; init; }

  /// <summary>
  /// OPTIONAL. A human-readable name for the root, suitable for display or for referencing the root
  /// elsewhere in an application (spec §21.1.5). When absent, no display name is implied.
  /// </summary>
  public string? Name { get; init; }

  /// <summary>
  /// OPTIONAL. Implementation-defined metadata attached to the root (spec §21.1.5/§4). A receiver
  /// MUST ignore <c>_meta</c> members it does not recognize.
  /// </summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// The result a client supplies, on retry, in response to a <c>roots/list</c> input request
/// (spec §21.1.5). The server requests the listing by returning an input-required result carrying
/// the <c>roots/list</c> input request; the client answers by retrying the originating request with
/// this result attached (the multi-round-trip mechanism of §11).
/// </summary>
/// <remarks>
/// This type belongs to the <b>Deprecated</b> Roots capability (spec §21.1) and is retained for
/// interoperability only.
/// </remarks>
public sealed record ListRootsResult
{
  /// <summary>The JSON-RPC method name of the input request answered by this result (spec §21.1.4).</summary>
  public const string Method = "roots/list";

  /// <summary>
  /// REQUIRED. The array of roots the client exposes (spec §21.1.5). The array MAY be empty
  /// (<c>[]</c>) to indicate the client exposes no roots, but it MUST be present even when empty.
  /// </summary>
  public required IReadOnlyList<Root> Roots { get; init; }
}
