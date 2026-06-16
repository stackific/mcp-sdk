using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Stackific.Mcp.JsonRpc;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// A progress-correlation token (spec §15.1.1): a value that is either a JSON string or a JSON
/// number, never <c>null</c>. The token is carried in an originating request's
/// <c>_meta.progressToken</c> (§15.1.2) and is echoed on every <see cref="ProgressNotificationParams"/>
/// so that out-of-band progress can be matched to the request that opted in.
/// </summary>
/// <remarks>
/// The wire type is preserved exactly — a numeric token round-trips as a number and a string
/// token as a string, and the two are never coerced into one another. Receivers MUST treat the
/// token as opaque (§15.1.1).
/// </remarks>
[JsonConverter(typeof(ProgressTokenJsonConverter))]
public readonly record struct ProgressToken
{
  private readonly string? _string;
  private readonly double _number;
  private readonly Kind _kind;

  private enum Kind : byte { Unset = 0, String = 1, Number = 2 }

  /// <summary>Creates a string-valued progress token.</summary>
  /// <param name="value">The non-null string token.</param>
  public ProgressToken(string value)
  {
    _string = value ?? throw new ArgumentNullException(nameof(value));
    _number = 0;
    _kind = Kind.String;
  }

  /// <summary>Creates an integer-valued progress token.</summary>
  /// <param name="value">The integer token.</param>
  public ProgressToken(long value)
  {
    _string = null;
    _number = value;
    _kind = Kind.Number;
  }

  /// <summary>Creates a number-valued progress token (used for non-integral wire numbers).</summary>
  /// <param name="value">The numeric token.</param>
  public ProgressToken(double value)
  {
    _string = null;
    _number = value;
    _kind = Kind.Number;
  }

  /// <summary><c>true</c> if this token carries a JSON string.</summary>
  public bool IsString => _kind == Kind.String;

  /// <summary><c>true</c> if this token carries a JSON number.</summary>
  public bool IsNumber => _kind == Kind.Number;

  /// <summary>Implicitly wraps an integer as a <see cref="ProgressToken"/>.</summary>
  /// <param name="value">The integer token.</param>
  public static implicit operator ProgressToken(long value) => new(value);

  /// <summary>Implicitly wraps a string as a <see cref="ProgressToken"/>.</summary>
  /// <param name="value">The string token.</param>
  public static implicit operator ProgressToken(string value) => new(value);

  /// <summary>
  /// Renders the token as a stable correlation key. Numeric tokens that are integral are
  /// rendered without a decimal point so they match how they are written back to the wire.
  /// </summary>
  /// <returns>The string form of the token.</returns>
  public override string ToString() => _kind switch
  {
    Kind.String => _string!,
    Kind.Number => IsIntegral(_number)
      ? ((long)_number).ToString(CultureInfo.InvariantCulture)
      : _number.ToString("R", CultureInfo.InvariantCulture),
    _ => string.Empty,
  };

  /// <summary>Materializes this token as a JSON node for inclusion in a message object.</summary>
  /// <returns>A <see cref="JsonValue"/> carrying the string or number.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the token is uninitialized.</exception>
  public JsonNode ToJsonNode() => _kind switch
  {
    Kind.String => JsonValue.Create(_string)!,
    Kind.Number => IsIntegral(_number) ? JsonValue.Create((long)_number) : JsonValue.Create(_number),
    _ => throw new InvalidOperationException("An uninitialized ProgressToken cannot be serialized."),
  };

  /// <summary>Reads a <see cref="ProgressToken"/> from a JSON node, enforcing the string/number rule.</summary>
  /// <param name="node">The token node (a string or number; never <c>null</c>).</param>
  /// <returns>The parsed token.</returns>
  /// <exception cref="McpError">Thrown (-32602) when the node is not a string or number.</exception>
  public static ProgressToken FromJsonNode(JsonNode node)
  {
    if (node is JsonValue value)
    {
      switch (value.GetValueKind())
      {
        case JsonValueKind.String:
          return new ProgressToken(value.GetValue<string>());
        case JsonValueKind.Number:
          // Preserve integrality so the token is written back in the same shape.
          if (value.TryGetValue(out long asLong)) return new ProgressToken(asLong);
          return new ProgressToken(value.GetValue<double>());
      }
    }
    throw McpError.InvalidParams("A \"progressToken\" must be a JSON string or number (never null) (§15.1.1).");
  }

  private static bool IsIntegral(double value) =>
    value >= long.MinValue && value <= long.MaxValue && Math.Floor(value) == value;

  internal void Write(Utf8JsonWriter writer)
  {
    switch (_kind)
    {
      case Kind.String:
        writer.WriteStringValue(_string);
        break;
      case Kind.Number:
        if (IsIntegral(_number)) writer.WriteNumberValue((long)_number);
        else writer.WriteNumberValue(_number);
        break;
      default:
        throw new InvalidOperationException("An uninitialized ProgressToken cannot be serialized.");
    }
  }
}

/// <summary>System.Text.Json converter that reads/writes a <see cref="ProgressToken"/> as a bare string or number.</summary>
internal sealed class ProgressTokenJsonConverter : JsonConverter<ProgressToken>
{
  /// <inheritdoc/>
  public override ProgressToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    reader.TokenType switch
    {
      JsonTokenType.String => new ProgressToken(reader.GetString()!),
      JsonTokenType.Number => reader.TryGetInt64(out var asLong) ? new ProgressToken(asLong) : new ProgressToken(reader.GetDouble()),
      _ => throw new JsonException("A \"progressToken\" must be a JSON string or number (never null)."),
    };

  /// <inheritdoc/>
  public override void Write(Utf8JsonWriter writer, ProgressToken value, JsonSerializerOptions options) => value.Write(writer);
}

/// <summary>
/// Parameters of the <c>notifications/progress</c> notification (spec §15.1.3), sent by the party
/// that is processing a request to report incremental progress on it. The notification is
/// request-scoped: it MUST reference a token the peer supplied in an active request's
/// <c>_meta.progressToken</c>, and MUST be sent before that request's final response (§15.1.4).
/// </summary>
public sealed record ProgressNotificationParams
{
  /// <summary>The method name of the notification these parameters belong to.</summary>
  public const string Method = "notifications/progress";

  /// <summary>REQUIRED. The token from the originating request's <c>_meta</c> that this update correlates to (§15.1.3).</summary>
  public required ProgressToken ProgressToken { get; init; }

  /// <summary>
  /// REQUIRED. The amount of progress so far. It MUST strictly increase across successive
  /// notifications for the same token, even when <see cref="Total"/> is unknown, and MAY be
  /// integral or fractional (§15.1.3).
  /// </summary>
  public required double Progress { get; init; }

  /// <summary>OPTIONAL. The total amount of progress expected, when known; MAY be integral or fractional (§15.1.3).</summary>
  public double? Total { get; init; }

  /// <summary>OPTIONAL. A human-readable description of the current progress, suitable for display (§15.1.3).</summary>
  public string? Message { get; init; }

  /// <summary>OPTIONAL. Notification metadata (§4 / §14).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// Parameters of the <c>notifications/cancelled</c> notification (spec §15.2.1), sent by a party to
/// cancel an in-flight request that it issued earlier in the same direction. Cancellation is
/// best-effort and races are tolerated: a receiver MAY ignore it when the request is unknown,
/// already complete, or uncancellable, and the canceller SHOULD ignore any late response (§15.2.2/§15.2.3).
/// </summary>
public sealed record CancelledNotificationParams
{
  /// <summary>The method name of the notification these parameters belong to.</summary>
  public const string Method = "notifications/cancelled";

  /// <summary>
  /// The JSON-RPC <c>id</c> of the request to cancel. It MUST correspond to a request the sender
  /// issued earlier in the same direction and believes is still in-flight (§15.2.1).
  /// </summary>
  public RequestId RequestId { get; init; }

  /// <summary>OPTIONAL. A human-readable explanation that MAY be logged or shown to a user (§15.2.1).</summary>
  public string? Reason { get; init; }

  /// <summary>OPTIONAL. Notification metadata (§4 / §14).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// A structured log severity (spec §15.3.1), mapping to the standard syslog message severities.
/// The members are declared from least to most severe; that ordering is significant because a
/// request opts in at a minimum level and the emitter MUST emit only messages at or above it.
/// </summary>
/// <remarks>
/// Deprecated [SEP-2577]: the logging-message mechanism of §15.3 is Deprecated and retained only
/// for interoperability with peers that emit it.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<LoggingLevel>))]
public enum LoggingLevel
{
  /// <summary>Detailed debugging information (lowest severity).</summary>
  [JsonStringEnumMemberName("debug")]
  Debug,

  /// <summary>General informational messages.</summary>
  [JsonStringEnumMemberName("info")]
  Info,

  /// <summary>Normal but significant events.</summary>
  [JsonStringEnumMemberName("notice")]
  Notice,

  /// <summary>Warning conditions.</summary>
  [JsonStringEnumMemberName("warning")]
  Warning,

  /// <summary>Error conditions.</summary>
  [JsonStringEnumMemberName("error")]
  Error,

  /// <summary>Critical conditions.</summary>
  [JsonStringEnumMemberName("critical")]
  Critical,

  /// <summary>Action must be taken immediately.</summary>
  [JsonStringEnumMemberName("alert")]
  Alert,

  /// <summary>System is unusable (highest severity).</summary>
  [JsonStringEnumMemberName("emergency")]
  Emergency,
}

/// <summary>
/// Parameters of the <c>notifications/message</c> notification (spec §15.3.2), by which a server
/// emits a structured log message. The notification is request-scoped and emitted only when the
/// request opted in via <c>_meta.io.modelcontextprotocol/logLevel</c>, at or above that level (§15.3.3).
/// </summary>
/// <remarks>
/// Deprecated [SEP-2577]. Log <see cref="Data"/> MUST NOT contain credentials, secrets, personally
/// identifying information, or internal details that could aid an attacker (§15.3.2).
/// </remarks>
public sealed record LoggingMessageNotificationParams
{
  /// <summary>The method name of the notification these parameters belong to.</summary>
  public const string Method = "notifications/message";

  /// <summary>REQUIRED. The severity of the message, one of the §15.3.1 levels.</summary>
  public required LoggingLevel Level { get; init; }

  /// <summary>OPTIONAL. A name identifying the logger that issued the message (§15.3.2).</summary>
  public string? Logger { get; init; }

  /// <summary>REQUIRED. The payload to be logged; any JSON-serializable value is allowed (§15.3.2).</summary>
  public required JsonNode Data { get; init; }

  /// <summary>OPTIONAL. Notification metadata (§4 / §14).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

// §15.4 Trace Context is propagated through three reserved bare `_meta` keys —
// `traceparent`, `tracestate`, and `baggage` — already defined as
// Stackific.Mcp.Json.MetaKeys.TraceParent / TraceState / Baggage. They are carried verbatim on
// any request's or notification's `_meta` (see RequestMeta.Additional) and are NOT redefined here.
