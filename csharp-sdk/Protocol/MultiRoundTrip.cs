using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The values of the base result discriminator <c>resultType</c> (spec §3.6, §11.2). The set is
/// open, but two values are defined by the core protocol; extensions (for example Tasks) mint more.
/// </summary>
public static class ResultTypes
{
  /// <summary>The request completed and the result carries the final content (§3.6).</summary>
  public const string Complete = "complete";

  /// <summary>The request needs further client input before it can complete (§11).</summary>
  public const string InputRequired = "input_required";

  /// <summary>The Tasks-extension augmented result: a task handle was returned in place of a result (§25.3).</summary>
  public const string Task = "task";
}

/// <summary>
/// A single input request a server embeds in an <see cref="InputRequiredResult"/> for the client to
/// fulfill (spec §11.2). The kind is discriminated by <see cref="Method"/>:
/// <c>elicitation/create</c> (§20), <c>sampling/createMessage</c> (§21), or <c>roots/list</c> (§21).
/// The runtime dispatches on the method name, so <see cref="Params"/> is kept as raw JSON.
/// </summary>
public sealed record InputRequest
{
  /// <summary>REQUIRED. The input-request kind: one of the three method names above.</summary>
  public required string Method { get; init; }

  /// <summary>OPTIONAL. The kind-specific parameters (an <c>ElicitRequestParams</c>, etc.).</summary>
  public JsonObject? Params { get; init; }
}

/// <summary>
/// The <c>input_required</c> result a server returns to solicit client input while processing a
/// request (spec §11.2). At least one of <see cref="InputRequests"/> or <see cref="RequestState"/>
/// MUST be present; a result with only <see cref="RequestState"/> is the load-shedding (retry-later)
/// signal (§11.5). The base <c>resultType</c> is supplied by the runtime as <c>input_required</c>.
/// </summary>
public sealed record InputRequiredResult
{
  /// <summary>OPTIONAL. A non-empty map from server-chosen key to the input request to fulfill.</summary>
  public IDictionary<string, InputRequest>? InputRequests { get; init; }

  /// <summary>OPTIONAL (in practice required for continuation). The opaque server continuation token, echoed verbatim on retry (§11.3).</summary>
  public string? RequestState { get; init; }
}
