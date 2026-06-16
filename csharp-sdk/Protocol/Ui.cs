using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Stackific.Mcp.Json;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// Constants for the Interactive User-Interface extension (spec §26): the UI resource MIME type
/// (§26.4) and the extension identifier used to negotiate the extension (§26.2).
/// </summary>
public static class UiResource
{
  /// <summary>
  /// The MIME type a UI resource MUST be served with (§26.4). Reproduced verbatim and matched
  /// case-sensitively, including the <c>;profile=mcp-app</c> profile parameter and the absence of
  /// surrounding whitespace; a host advertises this exact string in its <c>mimeTypes</c> (§26.2).
  /// </summary>
  public const string MimeType = "text/html;profile=mcp-app";

  /// <summary>
  /// The extension identifier <c>io.modelcontextprotocol/ui</c> (§26.2), used as the key under the
  /// <c>extensions</c> capability map; treated as an opaque, case-sensitive string.
  /// </summary>
  public const string ExtensionId = MetaKeys.UiExtension;
}

/// <summary>
/// The audiences a tool with a declared UI is exposed to (spec §26.3 <c>visibility</c>). When the
/// <c>visibility</c> array is omitted it is treated as both <see cref="Model"/> and <see cref="App"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UiVisibility>))]
public enum UiVisibility
{
  /// <summary>The tool is visible to, and callable by, the model/agent through the ordinary tool-calling flow (§26.3, §16).</summary>
  [JsonStringEnumMemberName("model")]
  Model,

  /// <summary>The tool is callable by the rendered UI through the message-channel dialect, subject to host mediation and consent (§26.3, §26.5, §26.7).</summary>
  [JsonStringEnumMemberName("app")]
  App,
}

/// <summary>
/// The object placed under a tool's <c>_meta.ui</c> key to declare its associated interactive user
/// interface (spec §26.3). A receiver that has not negotiated the extension MUST ignore this key (§24).
/// </summary>
public sealed record ToolUiMeta
{
  /// <summary>
  /// REQUIRED. A URI in the <c>ui://</c> scheme identifying the UI resource to render for this tool;
  /// the host obtains it by issuing <c>resources/read</c> for this exact URI (§26.3, §26.4, §17).
  /// </summary>
  public required string ResourceUri { get; init; }

  /// <summary>
  /// OPTIONAL. The audiences the tool is exposed to; elements are drawn from <see cref="UiVisibility"/>.
  /// When omitted the value is treated as <c>["model", "app"]</c> (§26.3).
  /// </summary>
  public IReadOnlyList<UiVisibility>? Visibility { get; init; }
}

/// <summary>
/// A content-security-policy descriptor for a UI resource (spec §26.4). Each member lists origin
/// strings; an origin not present in the applicable member MUST be blocked by the host. When the
/// whole <see cref="ResourceUiMeta.Csp"/> is omitted the host applies a deny-by-default policy (§26.7).
/// </summary>
public sealed record UiContentSecurityPolicy
{
  /// <summary>OPTIONAL. Origins the UI MAY open network connections to (§26.4).</summary>
  public IReadOnlyList<string>? ConnectDomains { get; init; }

  /// <summary>OPTIONAL. Origins the UI MAY load resources (scripts, stylesheets, images, media) from (§26.4).</summary>
  public IReadOnlyList<string>? ResourceDomains { get; init; }

  /// <summary>OPTIONAL. Origins the UI MAY embed in nested frames (§26.4).</summary>
  public IReadOnlyList<string>? FrameDomains { get; init; }

  /// <summary>OPTIONAL. Origins permitted as the document base URI (§26.4).</summary>
  public IReadOnlyList<string>? BaseUriDomains { get; init; }
}

/// <summary>
/// The sandbox permissions a UI resource requests (spec §26.4). Each member's <em>presence</em>
/// requests that capability and its value is an empty object; the host MUST NOT grant a capability
/// that is not requested and MAY decline a requested one (§26.7). Absent members are <c>null</c>.
/// </summary>
public sealed record UiPermissions
{
  /// <summary>OPTIONAL. Present (an empty object) to request camera access (§26.4).</summary>
  public JsonObject? Camera { get; init; }

  /// <summary>OPTIONAL. Present (an empty object) to request microphone access (§26.4).</summary>
  public JsonObject? Microphone { get; init; }

  /// <summary>OPTIONAL. Present (an empty object) to request geolocation access (§26.4).</summary>
  public JsonObject? Geolocation { get; init; }

  /// <summary>OPTIONAL. Present (an empty object) to request clipboard-write access (§26.4).</summary>
  public JsonObject? ClipboardWrite { get; init; }
}

/// <summary>
/// Presentation and security hints carried under a UI resource <c>contents</c> entry's own
/// <c>_meta.ui</c> object (spec §26.4). When present on the resource these hints take effect for
/// rendering; all fields are OPTIONAL.
/// </summary>
public sealed record ResourceUiMeta
{
  /// <summary>OPTIONAL. The origins the UI may contact, load resources from, frame, or use as a base URI (§26.4).</summary>
  public UiContentSecurityPolicy? Csp { get; init; }

  /// <summary>OPTIONAL. The sandbox permissions the UI requests (§26.4).</summary>
  public UiPermissions? Permissions { get; init; }

  /// <summary>OPTIONAL. A dedicated origin under which the host SHOULD render the UI, isolating it from other UI resources (§26.4).</summary>
  public string? Domain { get; init; }

  /// <summary>OPTIONAL. The server's preference that the host render a visible border around the UI; the host MAY ignore it (§26.4).</summary>
  public bool? PrefersBorder { get; init; }
}

/// <summary>
/// The value a host advertises under the <c>io.modelcontextprotocol/ui</c> key of its
/// <c>extensions</c> capability map (spec §26.2). A server MUST NOT declare UI associations unless a
/// host has advertised this with a <see cref="MimeTypes"/> array that includes <see cref="UiResource.MimeType"/>.
/// </summary>
public sealed record UiHostExtensionCapability
{
  /// <summary>
  /// REQUIRED. The UI resource MIME types the host can render as interactive user interfaces. A host
  /// supporting this extension MUST include the exact string <see cref="UiResource.MimeType"/>,
  /// matched verbatim and case-sensitively (§26.2).
  /// </summary>
  public required IReadOnlyList<string> MimeTypes { get; init; }
}
