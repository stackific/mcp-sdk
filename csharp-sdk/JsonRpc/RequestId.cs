using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.JsonRpc;

/// <summary>
/// A JSON-RPC request identifier (spec §3.2): either a JSON string or a JSON number, and
/// never <c>null</c>. The wire type is preserved exactly — a numeric id round-trips as a
/// number and a string id as a string, and the two are never coerced into one another.
/// </summary>
[JsonConverter(typeof(RequestIdJsonConverter))]
public readonly struct RequestId : IEquatable<RequestId>
{
  private readonly string? _string;
  private readonly double _number;
  private readonly Kind _kind;

  private enum Kind : byte { Unset = 0, String = 1, Number = 2 }

  /// <summary>Creates a string-valued identifier.</summary>
  /// <param name="value">The non-null string id.</param>
  public RequestId(string value)
  {
    _string = value ?? throw new ArgumentNullException(nameof(value));
    _number = 0;
    _kind = Kind.String;
  }

  /// <summary>Creates an integer-valued identifier.</summary>
  /// <param name="value">The integer id.</param>
  public RequestId(long value)
  {
    _string = null;
    _number = value;
    _kind = Kind.Number;
  }

  /// <summary>Creates a number-valued identifier (used for non-integral wire numbers).</summary>
  /// <param name="value">The numeric id.</param>
  public RequestId(double value)
  {
    _string = null;
    _number = value;
    _kind = Kind.Number;
  }

  /// <summary><c>true</c> if this identifier carries a JSON string.</summary>
  public bool IsString => _kind == Kind.String;

  /// <summary><c>true</c> if this identifier carries a JSON number.</summary>
  public bool IsNumber => _kind == Kind.Number;

  /// <summary>Implicitly wraps an integer as a <see cref="RequestId"/>.</summary>
  /// <param name="value">The integer id.</param>
  public static implicit operator RequestId(long value) => new(value);

  /// <summary>Implicitly wraps a string as a <see cref="RequestId"/>.</summary>
  /// <param name="value">The string id.</param>
  public static implicit operator RequestId(string value) => new(value);

  /// <inheritdoc/>
  public bool Equals(RequestId other)
  {
    if (_kind != other._kind) return false;
    return _kind switch
    {
      Kind.String => string.Equals(_string, other._string, StringComparison.Ordinal),
      Kind.Number => _number.Equals(other._number),
      _ => true,
    };
  }

  /// <inheritdoc/>
  public override bool Equals(object? obj) => obj is RequestId other && Equals(other);

  /// <inheritdoc/>
  public override int GetHashCode() => _kind switch
  {
    Kind.String => HashCode.Combine(Kind.String, _string),
    Kind.Number => HashCode.Combine(Kind.Number, _number),
    _ => 0,
  };

  /// <summary>Compares two identifiers for value equality (preserving JSON type).</summary>
  /// <param name="left">The left id.</param>
  /// <param name="right">The right id.</param>
  /// <returns><c>true</c> when equal.</returns>
  public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);

  /// <summary>Compares two identifiers for inequality.</summary>
  /// <param name="left">The left id.</param>
  /// <param name="right">The right id.</param>
  /// <returns><c>true</c> when not equal.</returns>
  public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);

  /// <summary>
  /// Renders the identifier as a stable correlation key. Numeric ids that are integral are
  /// rendered without a decimal point so they match how they are written back to the wire.
  /// </summary>
  /// <returns>The string form of the identifier.</returns>
  public override string ToString() => _kind switch
  {
    Kind.String => _string!,
    Kind.Number => IsIntegral(_number)
      ? ((long)_number).ToString(CultureInfo.InvariantCulture)
      : _number.ToString("R", CultureInfo.InvariantCulture),
    _ => string.Empty,
  };

  /// <summary>Materializes this identifier as a JSON node for inclusion in a message object.</summary>
  /// <returns>A <see cref="JsonValue"/> carrying the string or number.</returns>
  internal JsonNode ToJsonNode() => _kind switch
  {
    Kind.String => JsonValue.Create(_string)!,
    Kind.Number => IsIntegral(_number) ? JsonValue.Create((long)_number) : JsonValue.Create(_number),
    _ => throw new InvalidOperationException("An uninitialized RequestId cannot be serialized."),
  };

  /// <summary>Reads a <see cref="RequestId"/> from a JSON node, enforcing the string/number rule.</summary>
  /// <param name="node">The id node (a string or number; never <c>null</c>).</param>
  /// <returns>The parsed identifier.</returns>
  /// <exception cref="McpError">Thrown (-32600) when the node is not a string or number.</exception>
  internal static RequestId FromJsonNode(JsonNode node)
  {
    if (node is JsonValue value)
    {
      switch (value.GetValueKind())
      {
        case JsonValueKind.String:
          return new RequestId(value.GetValue<string>());
        case JsonValueKind.Number:
          // Preserve integrality so the id is written back in the same shape.
          if (value.TryGetValue(out long asLong)) return new RequestId(asLong);
          return new RequestId(value.GetValue<double>());
      }
    }
    throw McpError.InvalidRequest("Request \"id\" must be a JSON string or number (never null).");
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
        throw new InvalidOperationException("An uninitialized RequestId cannot be serialized.");
    }
  }
}

/// <summary>System.Text.Json converter that reads/writes a <see cref="RequestId"/> as a bare string or number.</summary>
internal sealed class RequestIdJsonConverter : JsonConverter<RequestId>
{
  public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    reader.TokenType switch
    {
      JsonTokenType.String => new RequestId(reader.GetString()!),
      JsonTokenType.Number => reader.TryGetInt64(out var asLong) ? new RequestId(asLong) : new RequestId(reader.GetDouble()),
      _ => throw new JsonException("Request \"id\" must be a JSON string or number (never null)."),
    };

  public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options) => value.Write(writer);
}
