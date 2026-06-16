using System.Text.RegularExpressions;

namespace Stackific.Mcp.Json;

/// <summary>
/// The reserved <c>_meta</c> keys defined by the protocol, plus the structural
/// naming rules for any <c>_meta</c> key (spec §4.2, Appendix C).
/// </summary>
/// <remarks>
/// The SDK never rejects an inbound message merely for carrying an unknown <c>_meta</c>
/// key — §4.1 requires unknown keys to be ignored — so the validation helpers here exist
/// for constructing well-formed metadata and for diagnostics, not for gating receipt.
/// </remarks>
public static partial class MetaKeys
{
  /// <summary>The canonical reverse-DNS prefix for keys defined by the MCP spec.</summary>
  public const string CanonicalPrefix = "io.modelcontextprotocol/";

  /// <summary>Reserved key carrying the protocol revision on every client request (§4.3).</summary>
  public const string ProtocolVersion = "io.modelcontextprotocol/protocolVersion";

  /// <summary>Reserved key carrying the client <c>Implementation</c> on every client request (§4.3).</summary>
  public const string ClientInfo = "io.modelcontextprotocol/clientInfo";

  /// <summary>Reserved key carrying the per-request <c>ClientCapabilities</c> (§4.3).</summary>
  public const string ClientCapabilities = "io.modelcontextprotocol/clientCapabilities";

  /// <summary>Reserved key carrying the optional, deprecated per-request log level (§4.3, §15.3).</summary>
  public const string LogLevel = "io.modelcontextprotocol/logLevel";

  /// <summary>Reserved key correlating a notification with its subscription stream (§10).</summary>
  public const string SubscriptionId = "io.modelcontextprotocol/subscriptionId";

  /// <summary>Reserved bare key: out-of-band progress-correlation token (§15.1).</summary>
  public const string ProgressToken = "progressToken";

  /// <summary>Reserved bare key: W3C Trace Context <c>traceparent</c> (§4.2, §15.4).</summary>
  public const string TraceParent = "traceparent";

  /// <summary>Reserved bare key: W3C Trace Context <c>tracestate</c> (§4.2, §15.4).</summary>
  public const string TraceState = "tracestate";

  /// <summary>Reserved bare key: W3C Baggage (§4.2, §15.4).</summary>
  public const string Baggage = "baggage";

  /// <summary>Extension identifier for the Tasks extension (§25).</summary>
  public const string TasksExtension = "io.modelcontextprotocol/tasks";

  /// <summary>Extension identifier for the Interactive User-Interface extension (§26).</summary>
  public const string UiExtension = "io.modelcontextprotocol/ui";

  /// <summary>The four bare keys reserved by exception to the prefix rule (§4.2).</summary>
  private static readonly HashSet<string> ReservedBareKeys =
    new(StringComparer.Ordinal) { ProgressToken, TraceParent, TraceState, Baggage };

  /// <summary>
  /// Returns <c>true</c> if <paramref name="key"/> is structurally a valid <c>_meta</c>
  /// key per §4.2: an optional dot-separated, slash-terminated prefix followed by a name.
  /// </summary>
  /// <param name="key">The candidate key.</param>
  /// <returns><c>true</c> when the key is well-formed.</returns>
  public static bool IsValidKey(string key)
  {
    ArgumentNullException.ThrowIfNull(key);
    if (ReservedBareKeys.Contains(key)) return true;

    var slash = key.IndexOf('/');
    if (slash < 0)
    {
      // No prefix: the whole key is a bare name. Only the reserved bare keys above are
      // sanctioned without a prefix, but the name itself must still be well-formed.
      return NameRegex().IsMatch(key);
    }

    var prefix = key[..(slash + 1)];
    var name = key[(slash + 1)..];
    return PrefixRegex().IsMatch(prefix) && (name.Length == 0 || NameRegex().IsMatch(name));
  }

  /// <summary>
  /// Returns <c>true</c> if <paramref name="key"/> sits under a prefix reserved for the
  /// protocol — that is, a prefix whose <em>second label</em> is <c>modelcontextprotocol</c>
  /// or <c>mcp</c> (§4.2). Third parties MUST NOT mint keys under such a prefix.
  /// </summary>
  /// <param name="key">The candidate key.</param>
  /// <returns><c>true</c> when the key's prefix is protocol-reserved.</returns>
  public static bool IsReservedPrefix(string key)
  {
    ArgumentNullException.ThrowIfNull(key);
    var slash = key.IndexOf('/');
    if (slash <= 0) return false;
    var labels = key[..slash].Split('.');
    return labels.Length >= 2 &&
      (labels[1].Equals("modelcontextprotocol", StringComparison.Ordinal) ||
       labels[1].Equals("mcp", StringComparison.Ordinal));
  }

  // A prefix is one or more labels separated by '.', terminated by a single '/'.
  // Each label starts with an ASCII letter and ends with a letter or digit; interior
  // characters may be letters, digits, or hyphens.
  [GeneratedRegex(@"^([A-Za-z]([A-Za-z0-9-]*[A-Za-z0-9])?)(\.([A-Za-z]([A-Za-z0-9-]*[A-Za-z0-9])?))*/$")]
  private static partial Regex PrefixRegex();

  // A non-empty name begins and ends with an alphanumeric character; interior characters
  // may be alphanumeric, hyphen, underscore, or dot.
  [GeneratedRegex(@"^[A-Za-z0-9]([A-Za-z0-9._-]*[A-Za-z0-9])?$")]
  private static partial Regex NameRegex();
}
