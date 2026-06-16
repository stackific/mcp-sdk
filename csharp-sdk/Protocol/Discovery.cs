using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The result of the <c>server/discover</c> request (spec §5.3.2): the protocol revisions the
/// server supports, its advertised capabilities, its identity, and optional natural-language
/// instructions. It is a <c>Result</c>, so on the wire it also carries the base
/// <c>resultType</c> discriminator (normally <c>complete</c>); that is added by the runtime.
/// </summary>
public sealed record DiscoverResult
{
  /// <summary>REQUIRED. A non-empty list of protocol revisions the server accepts (§5.3.2).</summary>
  public required IReadOnlyList<string> SupportedVersions { get; init; }

  /// <summary>REQUIRED. The server's advertised capabilities (§6.3). Empty is valid.</summary>
  public required ServerCapabilities Capabilities { get; init; }

  /// <summary>REQUIRED. The server software identity (§14.3).</summary>
  public required Implementation ServerInfo { get; init; }

  /// <summary>
  /// OPTIONAL. Natural-language guidance describing the server and how to use it effectively,
  /// suitable for a host's model context (§5.3.2). Absent means no guidance.
  /// </summary>
  public string? Instructions { get; init; }
}
