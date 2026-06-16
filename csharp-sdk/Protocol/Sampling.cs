using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The discriminated union of content blocks carried by a <see cref="SamplingMessage"/>
/// (spec §21.2.6). The wire discriminator is the <c>type</c> field.
/// </summary>
/// <remarks>
/// This union is <b>separate</b> from <see cref="ContentBlock"/> (§14.4): it adds the
/// sampling-only <c>tool_use</c> (<see cref="ToolUseContent"/>) and <c>tool_result</c>
/// (<see cref="ToolResultContent"/>) blocks and EXCLUDES <c>resource_link</c>/<c>resource</c>.
/// It belongs to the <b>Deprecated</b> Sampling capability (spec §21.2); implementations SHOULD
/// NOT adopt it for new functionality and SHOULD instead integrate directly with a model provider.
/// It remains defined for interoperability.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SamplingTextContent), "text")]
[JsonDerivedType(typeof(SamplingImageContent), "image")]
[JsonDerivedType(typeof(SamplingAudioContent), "audio")]
[JsonDerivedType(typeof(ToolUseContent), "tool_use")]
[JsonDerivedType(typeof(ToolResultContent), "tool_result")]
public abstract record SamplingMessageContentBlock
{
  private protected SamplingMessageContentBlock() { }

  /// <summary>OPTIONAL. Implementation- and extension-specific metadata (spec §4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// Ergonomic factory helpers for building <see cref="SamplingMessageContentBlock"/> values
/// (spec §21.2.6) for the <b>Deprecated</b> Sampling capability (§21.2).
/// </summary>
public static class SamplingContentBlocks
{
  /// <summary>Creates a <see cref="SamplingTextContent"/> block.</summary>
  /// <param name="text">The text.</param>
  /// <returns>The block.</returns>
  public static SamplingTextContent Text(string text) => new() { Text = text };

  /// <summary>Creates a <see cref="SamplingImageContent"/> block from Base64 data.</summary>
  /// <param name="base64Data">Base64-encoded image bytes.</param>
  /// <param name="mimeType">The image MIME type (for example <c>image/png</c>).</param>
  /// <returns>The block.</returns>
  public static SamplingImageContent Image(string base64Data, string mimeType) =>
    new() { Data = base64Data, MimeType = mimeType };

  /// <summary>Creates a <see cref="SamplingAudioContent"/> block from Base64 data.</summary>
  /// <param name="base64Data">Base64-encoded audio bytes.</param>
  /// <param name="mimeType">The audio MIME type (for example <c>audio/wav</c>).</param>
  /// <returns>The block.</returns>
  public static SamplingAudioContent Audio(string base64Data, string mimeType) =>
    new() { Data = base64Data, MimeType = mimeType };
}

/// <summary>
/// Plain text content within a sampling message (spec §21.2.6; field definition §14.4.1).
/// </summary>
public sealed record SamplingTextContent : SamplingMessageContentBlock
{
  /// <summary>REQUIRED. The text content.</summary>
  public required string Text { get; init; }
}

/// <summary>
/// Base64-encoded image content with a MIME type within a sampling message
/// (spec §21.2.6; field definition §14.4.2).
/// </summary>
public sealed record SamplingImageContent : SamplingMessageContentBlock
{
  /// <summary>REQUIRED. Base64-encoded image bytes.</summary>
  public required string Data { get; init; }

  /// <summary>REQUIRED. The image MIME type.</summary>
  public required string MimeType { get; init; }
}

/// <summary>
/// Base64-encoded audio content with a MIME type within a sampling message
/// (spec §21.2.6; field definition §14.4.3).
/// </summary>
public sealed record SamplingAudioContent : SamplingMessageContentBlock
{
  /// <summary>REQUIRED. Base64-encoded audio bytes.</summary>
  public required string Data { get; init; }

  /// <summary>REQUIRED. The audio MIME type.</summary>
  public required string MimeType { get; init; }
}

/// <summary>
/// A request from the assistant to call a tool, carried as a sampling content block
/// (spec §21.2.6, <c>type: "tool_use"</c>).
/// </summary>
public sealed record ToolUseContent : SamplingMessageContentBlock
{
  /// <summary>
  /// REQUIRED. A unique identifier for this tool use, used to match tool results to their
  /// corresponding tool uses (spec §21.2.6).
  /// </summary>
  public required string Id { get; init; }

  /// <summary>REQUIRED. The name of the tool to call (spec §21.2.6).</summary>
  public required string Name { get; init; }

  /// <summary>
  /// REQUIRED. The arguments to pass to the tool, conforming to the tool's input schema
  /// (spec §21.2.6).
  /// </summary>
  public required JsonObject Input { get; init; }
}

/// <summary>
/// The result of a tool use, provided by the user back to the assistant, carried as a sampling
/// content block (spec §21.2.6, <c>type: "tool_result"</c>).
/// </summary>
public sealed record ToolResultContent : SamplingMessageContentBlock
{
  /// <summary>
  /// REQUIRED. The <see cref="ToolUseContent.Id"/> of the tool use this result corresponds to; it
  /// MUST match the <c>id</c> from a previous <see cref="ToolUseContent"/> (spec §21.2.6).
  /// </summary>
  public required string ToolUseId { get; init; }

  /// <summary>
  /// REQUIRED. The unstructured result content, using the content-block array form defined for tool
  /// results in §16; it MAY include text, images, audio, resource links, and embedded resources
  /// (spec §21.2.6).
  /// </summary>
  public required IReadOnlyList<ContentBlock> Content { get; init; }

  /// <summary>
  /// OPTIONAL. A structured result value of any JSON type (object, array, string, number, boolean,
  /// or null). If the tool defined an output schema (§16), this SHOULD conform to it (spec §21.2.6).
  /// </summary>
  public JsonNode? StructuredContent { get; init; }

  /// <summary>
  /// OPTIONAL (absent ⇒ <c>false</c>). Whether the tool use resulted in an error; when <c>true</c>,
  /// <see cref="Content"/> typically describes the error (spec §21.2.6).
  /// </summary>
  public bool? IsError { get; init; }
}

/// <summary>
/// A single message in a sampling conversation (spec §21.2.6) for the <b>Deprecated</b> Sampling
/// capability (§21.2).
/// </summary>
public sealed record SamplingMessage
{
  /// <summary>REQUIRED. The message role, either <c>user</c> or <c>assistant</c> (spec §21.2.6).</summary>
  public required Role Role { get; init; }

  /// <summary>
  /// REQUIRED. The message content (spec §21.2.6). On the wire this is either a single content block
  /// or an array of content blocks; this SDK models it as an array (a single-block message is the
  /// one-element case). Per §21.2.7, a <c>user</c> message containing tool results MUST contain
  /// ONLY <see cref="ToolResultContent"/> blocks.
  /// </summary>
  public required IReadOnlyList<SamplingMessageContentBlock> Content { get; init; }

  /// <summary>OPTIONAL. Reserved metadata (spec §21.2.6/§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>
/// A hint guiding model selection within <see cref="ModelPreferences"/> (spec §21.2.9). All hints
/// are advisory; the client MAY ignore them. If multiple hints are specified, the client MUST
/// evaluate them in order, taking the first match.
/// </summary>
public sealed record ModelHint
{
  /// <summary>
  /// OPTIONAL. A hint for a model name (spec §21.2.9). The client SHOULD treat this as a substring of
  /// a model name (for example <c>sonnet</c> matches <c>claude-3-5-sonnet-20241022</c>) and MAY map
  /// it to a different provider's model that fills a similar niche.
  /// </summary>
  public string? Name { get; init; }
}

/// <summary>
/// A server's advisory preferences for which model the client should select for a sampling request
/// (spec §21.2.9). All preferences are advisory: the client (or host) makes the final selection and
/// MAY ignore them.
/// </summary>
public sealed record ModelPreferences
{
  /// <summary>
  /// OPTIONAL. Hints to guide model selection, evaluated in order with the first match taken
  /// (spec §21.2.9). The client SHOULD prioritize hints over the numeric priorities.
  /// </summary>
  public IReadOnlyList<ModelHint>? Hints { get; init; }

  /// <summary>
  /// OPTIONAL (range 0 to 1 inclusive). How much to prioritize minimizing cost: <c>0</c> means cost
  /// is not important, <c>1</c> means cost is the most important factor (spec §21.2.9).
  /// </summary>
  public double? CostPriority { get; init; }

  /// <summary>
  /// OPTIONAL (range 0 to 1 inclusive). How much to prioritize sampling speed (low latency):
  /// <c>0</c> means speed is not important, <c>1</c> means speed is the most important factor
  /// (spec §21.2.9).
  /// </summary>
  public double? SpeedPriority { get; init; }

  /// <summary>
  /// OPTIONAL (range 0 to 1 inclusive). How much to prioritize intelligence and capability:
  /// <c>0</c> means intelligence is not important, <c>1</c> means it is the most important factor
  /// (spec §21.2.9).
  /// </summary>
  public double? IntelligencePriority { get; init; }
}

/// <summary>
/// Controls how the model uses tools during sampling (spec §21.2.5). The default behavior when the
/// request omits <c>toolChoice</c> is <c>{ "mode": "auto" }</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ToolChoiceMode>))]
public enum ToolChoiceMode
{
  /// <summary>The model decides whether to use tools. This is the default (spec §21.2.5).</summary>
  [JsonStringEnumMemberName("auto")]
  Auto,

  /// <summary>The model MUST use at least one tool before completing (spec §21.2.5).</summary>
  [JsonStringEnumMemberName("required")]
  Required,

  /// <summary>The model MUST NOT use any tools (spec §21.2.5).</summary>
  [JsonStringEnumMemberName("none")]
  None,
}

/// <summary>
/// Controls the model's tool-use behavior for a sampling request (spec §21.2.5). A client MUST
/// return an error if this is provided but the client did not declare the <c>sampling.tools</c>
/// capability (§21.2.4).
/// </summary>
public sealed record ToolChoice
{
  /// <summary>
  /// OPTIONAL. The tool-use mode (spec §21.2.5). When omitted, the default is
  /// <see cref="ToolChoiceMode.Auto"/>.
  /// </summary>
  public ToolChoiceMode? Mode { get; init; }
}

/// <summary>
/// A request to include context from one or more connected servers, attached to a sampling prompt
/// (spec §21.2.4, <c>includeContext</c>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IncludeContext>))]
public enum IncludeContext
{
  /// <summary>No additional context. This is the default when the field is omitted (spec §21.2.4).</summary>
  [JsonStringEnumMemberName("none")]
  None,

  /// <summary>
  /// Include context from the requesting server (spec §21.2.4). This value is <b>Deprecated</b>;
  /// a server SHOULD use it only if the client declared the <c>sampling.context</c> sub-capability.
  /// </summary>
  [JsonStringEnumMemberName("thisServer")]
  ThisServer,

  /// <summary>
  /// Include context from all connected servers (spec §21.2.4). This value is <b>Deprecated</b>;
  /// a server SHOULD use it only if the client declared the <c>sampling.context</c> sub-capability.
  /// </summary>
  [JsonStringEnumMemberName("allServers")]
  AllServers,
}

/// <summary>
/// The parameters of a <c>sampling/createMessage</c> input request (spec §21.2.4) for the
/// <b>Deprecated</b> Sampling capability (§21.2). Sampling lets a server obtain a language-model
/// completion by delegating the model call to the client; it is delivered as an input-required
/// result and answered by retrying the originating request (§11).
/// </summary>
public sealed record CreateMessageRequestParams
{
  /// <summary>The JSON-RPC method name of this input request (spec §21.2.4).</summary>
  public const string Method = "sampling/createMessage";

  /// <summary>
  /// REQUIRED. The conversation to sample from, ordered oldest to newest (spec §21.2.4). The list
  /// SHOULD NOT be retained between separate requests.
  /// </summary>
  public required IReadOnlyList<SamplingMessage> Messages { get; init; }

  /// <summary>
  /// OPTIONAL. The server's advisory preferences for which model to select; the client MAY ignore
  /// them (spec §21.2.4).
  /// </summary>
  public ModelPreferences? ModelPreferences { get; init; }

  /// <summary>
  /// OPTIONAL. A system prompt the server wants to use; the client MAY modify or ignore it without
  /// communicating the change to the server (spec §21.2.4).
  /// </summary>
  public string? SystemPrompt { get; init; }

  /// <summary>
  /// OPTIONAL. A request to include context from one or more connected servers (spec §21.2.4).
  /// The default when omitted is <see cref="IncludeContext.None"/>. Servers SHOULD omit this or use
  /// <see cref="IncludeContext.None"/>; the other values are Deprecated.
  /// </summary>
  public IncludeContext? IncludeContext { get; init; }

  /// <summary>
  /// OPTIONAL. Controls randomness; the valid range depends on the model provider. The client MAY
  /// modify or ignore it (spec §21.2.4).
  /// </summary>
  public double? Temperature { get; init; }

  /// <summary>
  /// REQUIRED. The requested maximum number of tokens to sample (spec §21.2.4). The client MAY
  /// sample fewer, but MUST respect this as an upper bound.
  /// </summary>
  public required long MaxTokens { get; init; }

  /// <summary>
  /// OPTIONAL. Sequences that, when generated, stop generation. The client MAY modify or ignore them
  /// (spec §21.2.4).
  /// </summary>
  public IReadOnlyList<string>? StopSequences { get; init; }

  /// <summary>
  /// OPTIONAL. Provider-specific parameters passed through to the model provider; the format is
  /// provider-specific and the client MAY modify or ignore it (spec §21.2.4).
  /// </summary>
  public JsonObject? Metadata { get; init; }

  /// <summary>
  /// OPTIONAL. Tools the model MAY use during generation, each using the <see cref="Tool"/> shape of
  /// §16; scoped to this request (spec §21.2.4). A client MUST return an error if this is provided
  /// but the client did not declare the <c>sampling.tools</c> capability.
  /// </summary>
  public IReadOnlyList<Tool>? Tools { get; init; }

  /// <summary>
  /// OPTIONAL. Controls how the model uses tools; the default when omitted is
  /// <c>{ "mode": "auto" }</c> (spec §21.2.4). A client MUST return an error if this is provided but
  /// the client did not declare the <c>sampling.tools</c> capability.
  /// </summary>
  public ToolChoice? ToolChoice { get; init; }
}

/// <summary>
/// The completion delivered back to the server, on retry, in response to a
/// <c>sampling/createMessage</c> input request (spec §21.2.8) for the <b>Deprecated</b> Sampling
/// capability (§21.2). The base <c>resultType</c> discriminator (§3) is supplied by the runtime.
/// </summary>
public sealed record CreateMessageResult
{
  /// <summary>
  /// REQUIRED. The role of the produced message, <c>user</c> or <c>assistant</c>; a completion is
  /// normally <c>assistant</c> (spec §21.2.8).
  /// </summary>
  public required Role Role { get; init; }

  /// <summary>
  /// REQUIRED. The produced content (spec §21.2.8). On the wire this is a single content block or an
  /// array of blocks; this SDK models it as an array (a single-block response is the one-element
  /// case). Tool-use requests are returned in the <c>assistant</c> role as <see cref="ToolUseContent"/>.
  /// </summary>
  public required IReadOnlyList<SamplingMessageContentBlock> Content { get; init; }

  /// <summary>REQUIRED. The name of the model that generated the message (spec §21.2.8).</summary>
  public required string Model { get; init; }

  /// <summary>
  /// OPTIONAL. The reason sampling stopped, if known (spec §21.2.8). This is an open string to allow
  /// provider-specific values; the standard values are <c>endTurn</c>, <c>stopSequence</c>,
  /// <c>maxTokens</c>, and <c>toolUse</c>.
  /// </summary>
  public string? StopReason { get; init; }

  /// <summary>OPTIONAL. Reserved metadata (spec §21.2.8/§4).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}
