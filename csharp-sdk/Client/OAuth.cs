using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Stackific.Mcp.JsonRpc;

namespace Stackific.Mcp.Client;

/// <summary>
/// An OAuth 2.1 client flow (spec §23) for an MCP <see cref="Transport.StreamableHttpClientTransport"/>:
/// PKCE (<c>S256</c>) generation, protected-resource → authorization-server metadata discovery
/// (RFC 9728 → RFC 8414), dynamic client registration (RFC 7591), the authorize-URL builder, the
/// authorization-code token exchange (RFC 8707 audience binding), and the redirect verification
/// (CSRF/mix-up defenses). The C# counterpart of ts-sdk's <c>client/oauth.ts</c>. Randomness and the
/// PKCE digest use <see cref="System.Security.Cryptography"/>; the issued token is supplied to the
/// transport via its <c>tokenProvider</c>.
/// </summary>
public static class OAuth
{
  private const string PkceMethod = "S256";

  /// <summary>A PKCE verifier/challenge pair (spec §23.5).</summary>
  /// <param name="CodeVerifier">The high-entropy verifier kept by the client.</param>
  /// <param name="CodeChallenge">The <c>S256</c> challenge derived from the verifier and sent to the AS.</param>
  public sealed record PkcePair(string CodeVerifier, string CodeChallenge);

  /// <summary>The discovered OAuth metadata for a protected MCP resource (spec §23.2–§23.3).</summary>
  /// <param name="Issuer">The authorization-server issuer URL.</param>
  /// <param name="ProtectedResource">The RFC 9728 protected-resource metadata document.</param>
  /// <param name="AuthorizationServer">The RFC 8414 authorization-server metadata document.</param>
  public sealed record OAuthMetadata(string Issuer, JsonObject ProtectedResource, JsonObject AuthorizationServer);

  /// <summary>A dynamically registered client (spec §23.4 / RFC 7591).</summary>
  /// <param name="ClientId">The issued client identifier.</param>
  /// <param name="ClientSecret">The issued client secret, when the AS returned one.</param>
  public sealed record RegisteredClient(string ClientId, string? ClientSecret);

  /// <summary>A token-endpoint response (spec §23.5).</summary>
  public sealed record TokenResponse
  {
    /// <summary>REQUIRED. The issued access token (the bearer credential).</summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>REQUIRED. The token type (for example <c>Bearer</c>).</summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    /// <summary>OPTIONAL. The granted scope (space-delimited).</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>OPTIONAL. The access token's lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }
  }

  /// <summary>
  /// Generates a PKCE <c>S256</c> pair (spec §23.5): a random 32-byte verifier (base64url) and the
  /// challenge <c>base64url(SHA256(verifier))</c>.
  /// </summary>
  /// <returns>The PKCE pair.</returns>
  public static PkcePair CreatePkcePair()
  {
    var verifierBytes = RandomNumberGenerator.GetBytes(32);
    var codeVerifier = Base64Url(verifierBytes);
    var digest = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
    return new PkcePair(codeVerifier, Base64Url(digest));
  }

  /// <summary>
  /// Discovers protected-resource metadata (RFC 9728) then authorization-server metadata (RFC 8414)
  /// (spec §23.2–§23.3): GETs <paramref name="resourceMetadataUrl"/>, reads
  /// <c>authorization_servers[0]</c> as the issuer, then GETs
  /// <c>{issuer}/.well-known/oauth-authorization-server</c>.
  /// </summary>
  /// <param name="http">The HTTP client to use.</param>
  /// <param name="resource">The canonical resource identifier (the protected MCP endpoint URL).</param>
  /// <param name="resourceMetadataUrl">The protected-resource metadata URL (from the 401 challenge).</param>
  /// <returns>The discovered metadata (issuer, protected-resource, authorization-server).</returns>
  public static async Task<OAuthMetadata> DiscoverOAuthMetadataAsync(HttpClient http, string resource, string resourceMetadataUrl)
  {
    _ = resource; // The resource is the discovery context; the explicit metadata URL drives the GET.
    var prm = await GetJsonObjectAsync(http, resourceMetadataUrl).ConfigureAwait(false);

    var issuer = (prm["authorization_servers"] as JsonArray)?.FirstOrDefault()?.GetValue<string>()
      ?? throw McpError.InvalidParams("protected-resource metadata lists no authorization_servers.");

    var asMetadataUrl = $"{issuer.TrimEnd('/')}/.well-known/oauth-authorization-server";
    var asMeta = await GetJsonObjectAsync(http, asMetadataUrl).ConfigureAwait(false);

    return new OAuthMetadata(issuer, prm, asMeta);
  }

  /// <summary>
  /// Performs dynamic client registration (spec §23.4 / RFC 7591): POSTs to the AS metadata's
  /// <c>registration_endpoint</c> with the client name, redirect URIs, the <c>authorization_code</c>
  /// grant, and <c>application_type: native</c>.
  /// </summary>
  /// <param name="http">The HTTP client to use.</param>
  /// <param name="asMeta">The authorization-server metadata.</param>
  /// <param name="clientName">The human-readable client name.</param>
  /// <param name="redirectUris">The client's redirect URIs.</param>
  /// <returns>The registered client (id and optional secret).</returns>
  public static async Task<RegisteredClient> RegisterClientAsync(
    HttpClient http,
    JsonObject asMeta,
    string clientName,
    IReadOnlyList<string> redirectUris)
  {
    var endpoint = asMeta["registration_endpoint"]?.GetValue<string>()
      ?? throw McpError.InvalidParams("authorization server has no registration_endpoint.");

    var uris = new JsonArray();
    foreach (var uri in redirectUris) uris.Add(uri);

    var body = new JsonObject
    {
      ["client_name"] = clientName,
      ["redirect_uris"] = uris,
      ["grant_types"] = new JsonArray("authorization_code"),
      ["application_type"] = "native",
    };

    using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
    using var response = await http.PostAsync(endpoint, content).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false)
      ?? throw McpError.InternalError("client registration returned an empty body.");

    var clientId = json["client_id"]?.GetValue<string>()
      ?? throw McpError.InternalError("client registration returned no client_id.");
    var clientSecret = json["client_secret"]?.GetValue<string>();

    return new RegisteredClient(clientId, clientSecret);
  }

  /// <summary>
  /// Builds the authorization-request URL (spec §23.5): the AS metadata's
  /// <c>authorization_endpoint</c> with <c>response_type=code</c>, the client id, redirect URI, scope,
  /// state, PKCE challenge (<c>code_challenge_method=S256</c>), and the RFC 8707 <c>resource</c>
  /// parameter for audience binding (§23.6).
  /// </summary>
  /// <param name="asMeta">The authorization-server metadata.</param>
  /// <param name="clientId">The client identifier.</param>
  /// <param name="redirectUri">The redirect URI the code is returned to.</param>
  /// <param name="resource">The protected resource (audience binding, §23.6).</param>
  /// <param name="scope">The requested scope.</param>
  /// <param name="state">The CSRF state to round-trip and verify (§23.5).</param>
  /// <param name="codeChallenge">The PKCE <c>S256</c> challenge.</param>
  /// <returns>The fully-formed authorization URL.</returns>
  public static string BuildAuthorizeUrl(
    JsonObject asMeta,
    string clientId,
    string redirectUri,
    string resource,
    string scope,
    string state,
    string codeChallenge)
  {
    var endpoint = asMeta["authorization_endpoint"]?.GetValue<string>()
      ?? throw McpError.InvalidParams("authorization server has no authorization_endpoint.");

    var query = new[]
    {
      ("response_type", "code"),
      ("client_id", clientId),
      ("redirect_uri", redirectUri),
      ("scope", scope),
      ("state", state),
      ("code_challenge", codeChallenge),
      ("code_challenge_method", PkceMethod),
      ("resource", resource),
    };

    var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
    var encoded = string.Join('&', query.Select(p => $"{Uri.EscapeDataString(p.Item1)}={Uri.EscapeDataString(p.Item2)}"));
    return $"{endpoint}{separator}{encoded}";
  }

  /// <summary>
  /// Exchanges an authorization code (+ PKCE verifier) for tokens (spec §23.5): POSTs a
  /// form-urlencoded body to the AS metadata's <c>token_endpoint</c> with
  /// <c>grant_type=authorization_code</c>, the code, the verifier, the redirect URI, the client id,
  /// and the RFC 8707 <c>resource</c> parameter so the token is audience-bound to this server (§23.6).
  /// </summary>
  /// <param name="http">The HTTP client to use.</param>
  /// <param name="asMeta">The authorization-server metadata.</param>
  /// <param name="clientId">The client identifier.</param>
  /// <param name="code">The authorization code returned to the redirect URI.</param>
  /// <param name="codeVerifier">The PKCE verifier that derived the challenge.</param>
  /// <param name="redirectUri">The redirect URI the code was returned to.</param>
  /// <param name="resource">The protected resource (audience binding, §23.6).</param>
  /// <returns>The token-endpoint response.</returns>
  public static async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
    HttpClient http,
    JsonObject asMeta,
    string clientId,
    string code,
    string codeVerifier,
    string redirectUri,
    string resource)
  {
    var endpoint = asMeta["token_endpoint"]?.GetValue<string>()
      ?? throw McpError.InvalidParams("authorization server has no token_endpoint.");

    using var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
      ["grant_type"] = "authorization_code",
      ["code"] = code,
      ["code_verifier"] = codeVerifier,
      ["redirect_uri"] = redirectUri,
      ["client_id"] = clientId,
      ["resource"] = resource,
    });

    using var response = await http.PostAsync(endpoint, form).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
    {
      throw McpError.InternalError($"token endpoint returned HTTP {(int)response.StatusCode}.");
    }

    return await response.Content.ReadFromJsonAsync<TokenResponse>(Stackific.Mcp.McpJson.Options).ConfigureAwait(false)
      ?? throw McpError.InternalError("token endpoint returned an unreadable body.");
  }

  /// <summary>
  /// Verifies the authorization redirect before redeeming the code (spec §23.5/§23.7): the returned
  /// <paramref name="returnedState"/> MUST equal <paramref name="sentState"/> (CSRF defense), and when
  /// the AS advertises the <c>iss</c> parameter the returned <paramref name="returnedIss"/> MUST equal
  /// <paramref name="issuer"/> (mix-up defense). Throws <see cref="McpError"/> (<c>-32602</c>) on any
  /// mismatch.
  /// </summary>
  /// <param name="sentState">The state value sent in the authorization request.</param>
  /// <param name="returnedState">The state value returned on the redirect.</param>
  /// <param name="issuer">The authorization-server issuer.</param>
  /// <param name="returnedIss">The <c>iss</c> value returned on the redirect, if any.</param>
  /// <param name="issParameterSupported">Whether the AS advertises the <c>iss</c> parameter.</param>
  public static void VerifyAuthorizationRedirect(
    string sentState,
    string? returnedState,
    string issuer,
    string? returnedIss,
    bool issParameterSupported)
  {
    if (!string.Equals(sentState, returnedState, StringComparison.Ordinal))
    {
      throw McpError.InvalidParams("OAuth redirect `state` mismatch — possible CSRF; refusing to redeem the code (§23.5).");
    }

    if (issParameterSupported)
    {
      if (returnedIss is null)
      {
        throw McpError.InvalidParams("AS advertises the iss parameter but the redirect carried no `iss` (§23.7).");
      }
      if (!string.Equals(returnedIss, issuer, StringComparison.Ordinal))
      {
        throw McpError.InvalidParams($"OAuth redirect `iss` \"{returnedIss}\" does not match issuer \"{issuer}\" — possible mix-up (§23.7).");
      }
    }
  }

  /// <summary>Base64url-encodes <paramref name="bytes"/> without padding (PKCE / token encoding).</summary>
  private static string Base64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

  private static async Task<JsonObject> GetJsonObjectAsync(HttpClient http, string url)
  {
    using var response = await http.GetAsync(url).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false)
      ?? throw McpError.InternalError($"metadata at {url} was empty or unreadable.");
  }
}
