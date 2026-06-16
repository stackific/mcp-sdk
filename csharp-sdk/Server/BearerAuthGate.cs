using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Http;

using Stackific.Mcp.Transport;

namespace Stackific.Mcp.Server;

/// <summary>
/// Server-side authorization glue (spec §23): turns a token-validation callback into an
/// <see cref="IMcpAuthGate"/> for the Streamable HTTP adapter — emitting the 401 +
/// <c>WWW-Authenticate</c> challenge (carrying <c>resource_metadata</c>) the spec requires — and
/// builds the RFC 9728 protected-resource metadata document. The C# counterpart of ts-sdk's
/// <c>server/auth.ts</c>.
/// </summary>
public static class AuthGates
{
  /// <summary>
  /// Builds an RFC 9728 protected-resource metadata document (spec §23.2): the canonical
  /// <c>resource</c> identifier, the <c>authorization_servers</c> that protect it, the
  /// <c>scopes_supported</c> it recognizes, and the bearer delivery methods it accepts (header only).
  /// </summary>
  /// <param name="resource">The canonical resource identifier (the MCP endpoint URL).</param>
  /// <param name="authorizationServers">The authorization-server issuer URLs that protect this resource.</param>
  /// <param name="scopes">The scopes the resource recognizes.</param>
  /// <returns>The protected-resource metadata as a <see cref="JsonObject"/>.</returns>
  public static JsonObject BuildProtectedResourceMetadata(
    string resource,
    IReadOnlyList<string> authorizationServers,
    IReadOnlyList<string> scopes)
  {
    var servers = new JsonArray();
    foreach (var server in authorizationServers) servers.Add(server);

    var scopesSupported = new JsonArray();
    foreach (var scope in scopes) scopesSupported.Add(scope);

    return new JsonObject
    {
      ["resource"] = resource,
      ["authorization_servers"] = servers,
      ["scopes_supported"] = scopesSupported,
      ["bearer_methods_supported"] = new JsonArray("header"),
    };
  }

  /// <summary>
  /// Builds an <see cref="IMcpAuthGate"/> that requires a valid <c>Bearer</c> token (spec §23.1). On a
  /// missing or invalid token it answers <c>401</c> with a <c>WWW-Authenticate: Bearer …</c> challenge
  /// carrying <paramref name="resourceMetadataUrl"/>; on a token whose audience does not equal
  /// <paramref name="expectedAudience"/> it rejects the request (§23.6 — a server MUST reject a token
  /// not issued for it and never forward it). On success it threads the validated
  /// <see cref="AuthInfo"/> into request processing (<c>ctx.AuthInfo</c>).
  /// </summary>
  /// <param name="resourceMetadataUrl">The protected-resource metadata URL advertised via <c>resource_metadata</c>.</param>
  /// <param name="expectedAudience">This resource's canonical identifier; the token's audience MUST equal it (§23.6).</param>
  /// <param name="validate">Validates a bearer token, returning the caller's identity, or <c>null</c> to reject.</param>
  /// <returns>The configured authorization gate.</returns>
  public static IMcpAuthGate Bearer(string resourceMetadataUrl, string expectedAudience, Func<string, AuthInfo?> validate) =>
    new BearerAuthGate(resourceMetadataUrl, expectedAudience, validate);

  /// <summary>
  /// The bearer-token authorization gate (spec §23): reads the <c>Authorization: Bearer &lt;token&gt;</c>
  /// header, validates the token, binds its audience to this resource, and surfaces the identity.
  /// </summary>
  private sealed class BearerAuthGate(string resourceMetadataUrl, string expectedAudience, Func<string, AuthInfo?> validate) : IMcpAuthGate
  {
    private const string BearerPrefix = "Bearer ";

    /// <inheritdoc/>
    public Task<AuthGateResult> AuthorizeAsync(HttpContext context)
    {
      var header = context.Request.Headers.Authorization.ToString();
      var token = header.StartsWith(BearerPrefix, StringComparison.Ordinal)
        ? header[BearerPrefix.Length..].Trim()
        : string.Empty;

      if (token.Length == 0)
      {
        return Task.FromResult(Challenge());
      }

      var authInfo = validate(token);
      if (authInfo is null)
      {
        return Task.FromResult(Challenge());
      }

      // Audience binding (§23.6): reject a token not issued for this resource.
      if (!string.Equals(authInfo.Audience, expectedAudience, StringComparison.Ordinal))
      {
        return Task.FromResult(Challenge());
      }

      return Task.FromResult(new AuthGateResult(true, authInfo));
    }

    private AuthGateResult Challenge() =>
      new(false, null, 401, $"Bearer resource_metadata=\"{resourceMetadataUrl}\"");
  }
}
