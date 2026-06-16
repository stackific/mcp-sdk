using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The intended sharing scope of a cacheable result (spec §13). <c>public</c> means any client
/// or intermediary MAY cache and serve the response to any user; <c>private</c> means only the
/// requesting user's client MAY cache it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CacheScope>))]
public enum CacheScope
{
  /// <summary>Cacheable and shareable across users and intermediaries.</summary>
  [JsonStringEnumMemberName("public")]
  Public,

  /// <summary>Cacheable only by the requesting user's client; never served to another user.</summary>
  [JsonStringEnumMemberName("private")]
  Private,
}

/// <summary>
/// Response-cache hints carried by a cacheable result (spec §13.1): a client-cache time-to-live
/// in milliseconds and the sharing scope. Both are REQUIRED on a cacheable result.
/// </summary>
/// <param name="TtlMs">How long the client MAY treat the result as fresh, in milliseconds (minimum 0).</param>
/// <param name="CacheScope">The sharing scope of the cached result.</param>
public sealed record CacheHints(long TtlMs, CacheScope CacheScope)
{
  /// <summary>The default hints for list results: cacheable, public, with a zero TTL (immediately stale).</summary>
  public static CacheHints None { get; } = new(0, CacheScope.Public);
}
