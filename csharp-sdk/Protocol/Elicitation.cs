using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The parameters of an <c>elicitation/create</c> input request (spec §20.2). Elicitation lets a
/// server request structured input from the user through the client; it is delivered as an
/// input-required result and answered by retrying the originating request (§11). This is a closed
/// union of two mode-specific shapes (spec §20.3) discriminated by the <c>mode</c> field:
/// <see cref="ElicitRequestFormParams"/> (<c>"form"</c>) and <see cref="ElicitRequestURLParams"/>
/// (<c>"url"</c>).
/// </summary>
/// <remarks>
/// The form-mode discriminator is OPTIONAL on the wire — a request with no <c>mode</c> field MUST be
/// treated as form mode (§20.3). Serialization therefore writes <c>"mode": "form"</c> explicitly,
/// while a deserializer that defaults a missing discriminator to <see cref="ElicitRequestFormParams"/>
/// preserves that backwards-compatibility rule.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(ElicitRequestFormParams), "form")]
[JsonDerivedType(typeof(ElicitRequestURLParams), "url")]
public abstract record ElicitRequestParams
{
  private protected ElicitRequestParams() { }

  /// <summary>REQUIRED. Human-readable text describing what is requested and/or why (spec §20.3).</summary>
  public required string Message { get; init; }
}

/// <summary>
/// Form-mode elicitation parameters (spec §20.3, <c>mode: "form"</c>): in-band structured data
/// collection against a restricted JSON Schema. The collected data IS exposed to the client, so a
/// server MUST NOT use form mode for sensitive information such as passwords, API keys, tokens, or
/// payment credentials (§20.7).
/// </summary>
public sealed record ElicitRequestFormParams : ElicitRequestParams
{
  /// <summary>
  /// REQUIRED. The restricted JSON Schema describing the fields to collect (spec §20.3/§20.4).
  /// Its root <c>type</c> MUST be <c>"object"</c>; <c>properties</c> is a flat, non-nested map from
  /// field name to a <c>PrimitiveSchemaDefinition</c> (string, number/integer, boolean, or enum —
  /// see §20.4); <c>required</c> is an OPTIONAL array of property names; <c>$schema</c> is an
  /// OPTIONAL dialect identifier. Modeled as a raw <see cref="JsonObject"/> (mirroring how
  /// <c>Tool.InputSchema</c> is represented) because it carries an open, restricted-Schema fragment.
  /// </summary>
  public required JsonObject RequestedSchema { get; init; }
}

/// <summary>
/// URL-mode elicitation parameters (spec §20.3, <c>mode: "url"</c>): out-of-band interaction via
/// navigation to a URL. Data other than the URL itself is NOT exposed to the client, making this
/// the mode a server MUST use for sensitive flows such as authorization or payment (§20.7).
/// </summary>
public sealed record ElicitRequestURLParams : ElicitRequestParams
{
  /// <summary>
  /// REQUIRED. An opaque correlation identifier that uniquely identifies the elicitation within the
  /// server's context (spec §20.3). The client MUST treat it as opaque; it correlates this request
  /// with the elicitation-complete notification of §20.6.
  /// </summary>
  public required string ElicitationId { get; init; }

  /// <summary>
  /// REQUIRED. The URL the user should navigate to (spec §20.3). MUST be a valid URI [RFC3986]
  /// containing a valid URL. The client MUST NOT pre-fetch it or open it without explicit user
  /// consent (§20.7).
  /// </summary>
  public required string Url { get; init; }
}

/// <summary>
/// The user's response action to an elicitation request (spec §20.5). Exactly one of these literals
/// is returned in <see cref="ElicitResult.Action"/>; the distinctions apply to both form and URL
/// modes.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ElicitationAction>))]
public enum ElicitationAction
{
  /// <summary>The user explicitly approved and submitted (spec §20.5). For form mode, the result carries the collected <c>content</c>; for URL mode it signals consent to the interaction, not its completion.</summary>
  [JsonStringEnumMemberName("accept")]
  Accept,

  /// <summary>The user explicitly declined the request (spec §20.5). <c>content</c> is typically omitted.</summary>
  [JsonStringEnumMemberName("decline")]
  Decline,

  /// <summary>The user dismissed without an explicit choice — for example closed the dialog, clicked away, pressed Escape, or the URL failed to load (spec §20.5). <c>content</c> is typically omitted.</summary>
  [JsonStringEnumMemberName("cancel")]
  Cancel,
}

/// <summary>
/// The client's response to an elicitation request, supplied as the input on retry (spec §20.5).
/// </summary>
public sealed record ElicitResult
{
  /// <summary>REQUIRED. The user's response action (spec §20.5).</summary>
  public required ElicitationAction Action { get; init; }

  /// <summary>
  /// OPTIONAL. The collected field values (spec §20.5). Present only when <see cref="Action"/> is
  /// <see cref="ElicitationAction.Accept"/> and the mode was form; omitted for URL-mode responses
  /// and typically for decline/cancel. When present, each value is a string, number, boolean, or
  /// array of strings, and the map MUST conform to the request's <c>requestedSchema</c>.
  /// </summary>
  public JsonObject? Content { get; init; }

  /// <summary>OPTIONAL. Implementation- and extension-specific metadata (spec §4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// The parameters of the <c>notifications/elicitation/complete</c> notification (spec §20.6), which a
/// server MAY send to signal that a URL-mode out-of-band interaction has completed. A client MUST
/// ignore a notification referencing an unknown or already-completed <see cref="ElicitationId"/>.
/// </summary>
public sealed record ElicitationCompleteNotificationParams
{
  /// <summary>The JSON-RPC method name of this notification (spec §20.6).</summary>
  public const string Method = "notifications/elicitation/complete";

  /// <summary>
  /// REQUIRED. The identifier of the elicitation that completed (spec §20.6). It MUST match the
  /// <see cref="ElicitRequestURLParams.ElicitationId"/> established in the original URL-mode
  /// <c>elicitation/create</c> request.
  /// </summary>
  public required string ElicitationId { get; init; }
}
