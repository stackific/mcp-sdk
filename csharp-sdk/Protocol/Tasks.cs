using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The lifecycle state of a task (spec §25.5). A task is created non-terminal (typically
/// <see cref="Working"/>) and moves among <see cref="Working"/> and <see cref="InputRequired"/>
/// until it reaches one of the terminal states <see cref="Completed"/>, <see cref="Failed"/>, or
/// <see cref="Cancelled"/>, after which its status and inline payload are immutable.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<McpTaskStatus>))]
public enum McpTaskStatus
{
  /// <summary>The operation is in progress (non-terminal) (§25.5).</summary>
  [JsonStringEnumMemberName("working")]
  Working,

  /// <summary>The server requires client input before it can continue (non-terminal); the outstanding requests are surfaced in <c>inputRequests</c> and resolved via <c>tasks/update</c> (§25.5, §25.8).</summary>
  [JsonStringEnumMemberName("input_required")]
  InputRequired,

  /// <summary>The operation finished successfully; the underlying result is carried inline in <c>result</c> (terminal) (§25.5).</summary>
  [JsonStringEnumMemberName("completed")]
  Completed,

  /// <summary>A JSON-RPC error occurred during execution; the error is carried inline in <c>error</c> (terminal) (§25.5).</summary>
  [JsonStringEnumMemberName("failed")]
  Failed,

  /// <summary>The operation ended in response to a cancellation request (terminal) (§25.5).</summary>
  [JsonStringEnumMemberName("cancelled")]
  Cancelled,
}

/// <summary>
/// The handle and status record for a long-running operation (spec §25.4). A <see cref="McpTask"/> is
/// returned immediately in place of a blocking result; the client polls <c>tasks/get</c> to observe
/// progress until the task reaches a terminal status. Identifiers are server-minted and opaque, and
/// the durable record survives client disconnects and restarts (§25.6). Named <c>McpTask</c> to avoid
/// clashing with <see cref="System.Threading.Tasks.Task"/>; the wire object is unaffected.
/// </summary>
public sealed record McpTask
{
  /// <summary>REQUIRED. Opaque, server-minted identifier for this task; the client MUST treat it verbatim and MUST NOT parse it (§25.4).</summary>
  public required string TaskId { get; init; }

  /// <summary>REQUIRED. The current lifecycle state (§25.4, §25.5).</summary>
  public required McpTaskStatus Status { get; init; }

  /// <summary>OPTIONAL. A human-readable, display-only description of the current state or progress; carries no protocol semantics (§25.4).</summary>
  public string? StatusMessage { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task was created (§25.4).</summary>
  public required string CreatedAt { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task state was last modified (§25.4).</summary>
  public required string LastUpdatedAt { get; init; }

  /// <summary>
  /// REQUIRED. Task lifetime in milliseconds measured from creation: a non-negative number, or
  /// <c>null</c> for an unbounded lifetime. After a non-null value has elapsed the server MAY
  /// discard the task and answer subsequent queries with the not-found error (§25.4, §25.11).
  /// </summary>
  public required long? TtlMs { get; init; }

  /// <summary>OPTIONAL. The recommended MINIMUM interval, in milliseconds, a client SHOULD wait between successive <c>tasks/get</c> polls; clients SHOULD NOT poll more frequently (§25.4, §25.7).</summary>
  public long? PollIntervalMs { get; init; }
}

/// <summary>
/// A <see cref="McpTask"/> that additionally conveys the terminal payload (or pending input requests)
/// inline (spec §25.4). It is the shape returned by <c>tasks/get</c> and carried on
/// <c>notifications/tasks</c>, modelled here as a single record whose status-specific members are
/// populated according to <c>status</c>: <see cref="InputRequests"/> for <c>input_required</c>,
/// <see cref="Result"/> for <c>completed</c>, and <see cref="Error"/> for <c>failed</c>. A
/// non-terminal task carries neither <see cref="Result"/> nor <see cref="Error"/> (§25.5).
/// </summary>
public sealed record DetailedTask
{
  /// <summary>REQUIRED. Opaque, server-minted identifier for this task (§25.4).</summary>
  public required string TaskId { get; init; }

  /// <summary>REQUIRED. The current lifecycle state that discriminates which status-specific member (if any) is present (§25.4, §25.5).</summary>
  public required McpTaskStatus Status { get; init; }

  /// <summary>OPTIONAL. A human-readable, display-only description of the current state or progress (§25.4).</summary>
  public string? StatusMessage { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task was created (§25.4).</summary>
  public required string CreatedAt { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task state was last modified (§25.4).</summary>
  public required string LastUpdatedAt { get; init; }

  /// <summary>REQUIRED. Task lifetime in milliseconds from creation, or <c>null</c> for unbounded (§25.4, §25.11).</summary>
  public required long? TtlMs { get; init; }

  /// <summary>OPTIONAL. The recommended MINIMUM polling interval in milliseconds (§25.4, §25.7).</summary>
  public long? PollIntervalMs { get; init; }

  /// <summary>
  /// Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.InputRequired"/>: the
  /// outstanding server-to-client requests the client must fulfill before the task can continue,
  /// keyed by opaque server-chosen identifier (§25.4). The map uses the same <c>InputRequest</c>
  /// shape as the in-line multi-round-trip flow (§11.2) and is resolved via <c>tasks/update</c>.
  /// </summary>
  public IDictionary<string, InputRequest>? InputRequests { get; init; }

  /// <summary>
  /// Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.Completed"/>: the verbatim
  /// ordinary result object the augmented request would have produced had it not been turned into
  /// a task — including that result's own <c>resultType</c> and any <c>_meta</c> (§25.4). For a
  /// tool call this is a <see cref="CallToolResult"/>; it is kept as raw JSON because the result
  /// shape depends on the underlying request.
  /// </summary>
  public JsonObject? Result { get; init; }

  /// <summary>Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.Failed"/>: the JSON-RPC error object that occurred during execution (§25.4, §22).</summary>
  public JsonObject? Error { get; init; }
}

/// <summary>
/// The augmented result a server returns in place of a request's ordinary result to signal that a
/// task handle was created (spec §25.3). On the wire it is a <c>Result</c> whose <c>resultType</c>
/// is the literal string <c>"task"</c> (supplied by the runtime; see <see cref="ResultTypes.Task"/>)
/// and which carries all <see cref="McpTask"/> fields directly. A client that declared the Tasks
/// capability MUST inspect <c>resultType</c> on each eligible response and handle the task case.
/// </summary>
public sealed record CreateTaskResult
{
  /// <summary>REQUIRED. Opaque, server-minted identifier for the newly created task (§25.3, §25.4).</summary>
  public required string TaskId { get; init; }

  /// <summary>REQUIRED. The initial lifecycle state of the task (typically <see cref="McpTaskStatus.Working"/>) (§25.3, §25.4).</summary>
  public required McpTaskStatus Status { get; init; }

  /// <summary>OPTIONAL. A human-readable, display-only description of the initial state (§25.4).</summary>
  public string? StatusMessage { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task was created (§25.4).</summary>
  public required string CreatedAt { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task state was last modified (§25.4).</summary>
  public required string LastUpdatedAt { get; init; }

  /// <summary>REQUIRED. Task lifetime in milliseconds from creation, or <c>null</c> for unbounded (§25.4, §25.11).</summary>
  public required long? TtlMs { get; init; }

  /// <summary>OPTIONAL. The recommended MINIMUM polling interval in milliseconds (§25.4, §25.7).</summary>
  public long? PollIntervalMs { get; init; }

  /// <summary>OPTIONAL. Implementation- and extension-specific metadata permitted on any result (§3, §14).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}

/// <summary>The parameters of a <c>tasks/get</c> request (spec §25.7): the polling primitive that retrieves a task's current state.</summary>
public sealed record GetTaskRequestParams
{
  /// <summary>REQUIRED. The server-generated identifier of the task to query, sent verbatim as obtained from the originating <see cref="CreateTaskResult"/> (§25.7).</summary>
  public required string TaskId { get; init; }
}

/// <summary>
/// The result of a <c>tasks/get</c> request (spec §25.7): a <see cref="DetailedTask"/> whose
/// status-specific payload fields are inlined for the task's current status. The runtime supplies
/// the base <c>resultType</c> as <c>"complete"</c> (see <see cref="ResultTypes.Complete"/>).
/// </summary>
public sealed record GetTaskResult
{
  /// <summary>REQUIRED. The detailed task for the queried <c>taskId</c>, carrying the status-specific payload for its current status (§25.7).</summary>
  public required DetailedTask Task { get; init; }
}

/// <summary>
/// The parameters of a <c>tasks/update</c> request (spec §25.8): supplies the responses to the
/// outstanding input requests of a task in the <see cref="McpTaskStatus.InputRequired"/> status.
/// </summary>
public sealed record UpdateTaskRequestParams
{
  /// <summary>REQUIRED. The identifier of the task whose outstanding input is being supplied (§25.8).</summary>
  public required string TaskId { get; init; }

  /// <summary>
  /// REQUIRED. Responses keyed by a currently-outstanding <c>inputRequests</c> key (§25.8). Each
  /// value is shaped as the response to the corresponding server-to-client request would be when
  /// surfaced inline (for example an elicitation result, §20); it is kept as raw JSON because the
  /// shape depends on the input-request kind. A server SHOULD ignore entries whose key is not
  /// currently outstanding.
  /// </summary>
  public required IDictionary<string, JsonNode> InputResponses { get; init; }
}

/// <summary>
/// The result of a <c>tasks/update</c> request (spec §25.8): an empty acknowledgement. The runtime
/// supplies the base <c>resultType</c> as <c>"complete"</c> (see <see cref="ResultTypes.Complete"/>).
/// The acknowledgement is eventually consistent: it MAY be returned before the task's observable
/// status reflects the responses.
/// </summary>
public sealed record UpdateTaskResult;

/// <summary>The parameters of a <c>tasks/cancel</c> request (spec §25.9): requests cooperative cancellation of an in-progress task.</summary>
public sealed record CancelTaskRequestParams
{
  /// <summary>REQUIRED. The identifier of the task to cancel (§25.9).</summary>
  public required string TaskId { get; init; }
}

/// <summary>
/// The result of a <c>tasks/cancel</c> request (spec §25.9): an empty acknowledgement. The runtime
/// supplies the base <c>resultType</c> as <c>"complete"</c> (see <see cref="ResultTypes.Complete"/>).
/// Cancellation is cooperative and eventually consistent: acknowledgement does not guarantee the
/// task will reach the <see cref="McpTaskStatus.Cancelled"/> terminal status.
/// </summary>
public sealed record CancelTaskResult;

/// <summary>
/// The parameters of the <c>notifications/tasks</c> notification (spec §25.10), by which a server
/// pushes a task state change. The params are a complete <see cref="DetailedTask"/> for the task's
/// current status — identical to what <c>tasks/get</c> would have returned at that moment — so a
/// subscribed client need not issue an extra <c>tasks/get</c>. Delivery is opt-in via a
/// <c>taskIds</c> subscription filter on <c>subscriptions/listen</c> (§10, §25.10).
/// </summary>
public sealed record TaskStatusNotificationParams
{
  /// <summary>The method name of the notification these parameters belong to (§25.10).</summary>
  public const string Method = "notifications/tasks";

  /// <summary>REQUIRED. Opaque, server-minted identifier of the task whose status changed (§25.4, §25.10).</summary>
  public required string TaskId { get; init; }

  /// <summary>REQUIRED. The current lifecycle state that discriminates which status-specific member (if any) is present (§25.4, §25.10).</summary>
  public required McpTaskStatus Status { get; init; }

  /// <summary>OPTIONAL. A human-readable, display-only description of the current state or progress (§25.4).</summary>
  public string? StatusMessage { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task was created (§25.4).</summary>
  public required string CreatedAt { get; init; }

  /// <summary>REQUIRED. ISO 8601 / RFC 3339 date-time string for when the task state was last modified (§25.4).</summary>
  public required string LastUpdatedAt { get; init; }

  /// <summary>REQUIRED. Task lifetime in milliseconds from creation, or <c>null</c> for unbounded (§25.4, §25.11).</summary>
  public required long? TtlMs { get; init; }

  /// <summary>OPTIONAL. The recommended MINIMUM polling interval in milliseconds (§25.4, §25.7).</summary>
  public long? PollIntervalMs { get; init; }

  /// <summary>Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.InputRequired"/>: the outstanding input requests, keyed by opaque identifier (§25.4, §25.10).</summary>
  public IDictionary<string, InputRequest>? InputRequests { get; init; }

  /// <summary>Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.Completed"/>: the verbatim underlying success result (§25.4, §25.10).</summary>
  public JsonObject? Result { get; init; }

  /// <summary>Present ONLY when <see cref="Status"/> is <see cref="McpTaskStatus.Failed"/>: the underlying JSON-RPC error (§25.4, §25.10, §22).</summary>
  public JsonObject? Error { get; init; }

  /// <summary>OPTIONAL. Notification metadata (§3, §14).</summary>
  [JsonPropertyName("_meta")]
  public JsonObject? Meta { get; init; }
}
