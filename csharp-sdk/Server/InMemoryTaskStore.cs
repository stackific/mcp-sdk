using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

using Stackific.Mcp.JsonRpc;
using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Server;

/// <summary>
/// An in-memory, thread-safe store for the Tasks extension (spec §25). It mints opaque task ids,
/// tracks lifecycle state with terminal-state immutability (§25.5), holds the inline result/error of
/// terminal tasks, and answers <c>tasks/get</c> with a <see cref="DetailedTask"/>. A <c>now</c> clock
/// is injectable for deterministic tests. Durability beyond the process lifetime is out of scope for
/// this in-memory implementation; a server needing it supplies its own store.
/// </summary>
public sealed class InMemoryTaskStore
{
  private readonly ConcurrentDictionary<string, Entry> _tasks = new(StringComparer.Ordinal);
  private readonly Func<DateTimeOffset> _now;

  /// <summary>Creates a store, optionally with an injected clock.</summary>
  /// <param name="now">A clock used for created/updated timestamps; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
  public InMemoryTaskStore(Func<DateTimeOffset>? now = null) => _now = now ?? (() => DateTimeOffset.UtcNow);

  /// <summary>The recommended minimum polling interval advertised to clients, in milliseconds (§25.4).</summary>
  public long PollIntervalMs { get; init; } = 500;

  /// <summary>Creates a new <see cref="McpTaskStatus.Working"/> task and returns its handle (§25.3).</summary>
  /// <param name="ttlMs">The task lifetime in milliseconds from creation, or <c>null</c> for unbounded.</param>
  /// <returns>The created task handle.</returns>
  public McpTask Create(long? ttlMs)
  {
    var id = Guid.NewGuid().ToString("N");
    var timestamp = _now().ToString("O");
    _tasks[id] = new Entry { Status = McpTaskStatus.Working, CreatedAt = timestamp, LastUpdatedAt = timestamp, TtlMs = ttlMs };
    return Snapshot(id);
  }

  /// <summary>The current status of a task, or <c>null</c> if it is unknown/expired (read defensively).</summary>
  /// <param name="taskId">The task id.</param>
  /// <returns>The status, or <c>null</c>.</returns>
  public McpTaskStatus? StatusOf(string taskId) => _tasks.TryGetValue(taskId, out var entry) ? entry.Status : null;

  /// <summary>Updates a non-terminal task's status and message (§25.5). Transitions out of a terminal state are ignored.</summary>
  /// <param name="taskId">The task id.</param>
  /// <param name="status">The new status.</param>
  /// <param name="message">An optional display-only status message.</param>
  public void UpdateStatus(string taskId, McpTaskStatus status, string? message = null)
  {
    if (!_tasks.TryGetValue(taskId, out var entry) || IsTerminal(entry.Status)) return;
    entry.Status = status;
    entry.StatusMessage = message;
    entry.LastUpdatedAt = _now().ToString("O");
  }

  /// <summary>Stores the successful terminal result of a task (§25.4/§25.5).</summary>
  /// <param name="taskId">The task id.</param>
  /// <param name="result">The underlying tool result; serialized with <c>resultType: complete</c> and stored inline.</param>
  public void StoreResult(string taskId, CallToolResult result)
  {
    if (!_tasks.TryGetValue(taskId, out var entry) || IsTerminal(entry.Status)) return;
    var resultObject = JsonSerializer.SerializeToNode(result, McpJson.Options)!.AsObject();
    resultObject["resultType"] = ResultTypes.Complete;
    entry.Result = resultObject;
    entry.Status = McpTaskStatus.Completed;
    entry.LastUpdatedAt = _now().ToString("O");
  }

  /// <summary>Marks a task as failed, storing the JSON-RPC error inline (§25.4/§25.5).</summary>
  /// <param name="taskId">The task id.</param>
  /// <param name="error">The error object.</param>
  public void Fail(string taskId, JsonRpcError error)
  {
    if (!_tasks.TryGetValue(taskId, out var entry) || IsTerminal(entry.Status)) return;
    entry.Error = new JsonObject { ["code"] = error.Code, ["message"] = error.Message };
    if (error.Data is not null) entry.Error["data"] = error.Data.DeepClone();
    entry.Status = McpTaskStatus.Failed;
    entry.LastUpdatedAt = _now().ToString("O");
  }

  /// <summary>Requests cancellation of a task (§25.9). A non-terminal task transitions to <see cref="McpTaskStatus.Cancelled"/>.</summary>
  /// <param name="taskId">The task id.</param>
  /// <returns><c>true</c> if the task existed.</returns>
  public bool Cancel(string taskId)
  {
    if (!_tasks.TryGetValue(taskId, out var entry)) return false;
    if (!IsTerminal(entry.Status))
    {
      entry.Status = McpTaskStatus.Cancelled;
      entry.LastUpdatedAt = _now().ToString("O");
    }
    return true;
  }

  /// <summary>Returns the detailed state of a task for <c>tasks/get</c> (§25.7).</summary>
  /// <param name="taskId">The task id.</param>
  /// <returns>The detailed task with its status-specific payload.</returns>
  /// <exception cref="McpError">-32602 when the task is unknown or expired (§25.7).</exception>
  public DetailedTask Get(string taskId)
  {
    if (!_tasks.TryGetValue(taskId, out var entry))
    {
      throw McpError.InvalidParams("Failed to retrieve task: Task not found.", new JsonObject { ["taskId"] = taskId });
    }
    return new DetailedTask
    {
      TaskId = taskId,
      Status = entry.Status,
      StatusMessage = entry.StatusMessage,
      CreatedAt = entry.CreatedAt,
      LastUpdatedAt = entry.LastUpdatedAt,
      TtlMs = entry.TtlMs,
      PollIntervalMs = PollIntervalMs,
      Result = entry.Status == McpTaskStatus.Completed ? entry.Result : null,
      Error = entry.Status == McpTaskStatus.Failed ? entry.Error : null,
    };
  }

  private McpTask Snapshot(string taskId)
  {
    var entry = _tasks[taskId];
    return new McpTask
    {
      TaskId = taskId,
      Status = entry.Status,
      StatusMessage = entry.StatusMessage,
      CreatedAt = entry.CreatedAt,
      LastUpdatedAt = entry.LastUpdatedAt,
      TtlMs = entry.TtlMs,
      PollIntervalMs = PollIntervalMs,
    };
  }

  private static bool IsTerminal(McpTaskStatus status) =>
    status is McpTaskStatus.Completed or McpTaskStatus.Failed or McpTaskStatus.Cancelled;

  private sealed class Entry
  {
    public McpTaskStatus Status;
    public string? StatusMessage;
    public required string CreatedAt;
    public required string LastUpdatedAt;
    public long? TtlMs;
    public JsonObject? Result;
    public JsonObject? Error;
  }
}
