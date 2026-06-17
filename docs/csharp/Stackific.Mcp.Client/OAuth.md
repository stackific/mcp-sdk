# OAuth class

An OAuth 2.1 client flow (spec §23) for an MCP [`StreamableHttpClientTransport`](../Stackific.Mcp.Transport/StreamableHttpClientTransport.md): PKCE (`S256`) generation and the §28.5 support gate, two-stage protected-resource → authorization-server metadata discovery with the RFC 8414/OIDC fallback ordering and the RFC 9207 issuer mix-up check, parsing of the `WWW-Authenticate` challenge on a `401`, dynamic client registration (RFC 7591) with retryable-failure surfacing, the authorize-URL builder, the authorization-code token exchange and the refresh-token exchange (RFC 8707 audience binding), and redirect verification (CSRF/mix-up defenses). The C# counterpart of ts-sdk's `client/oauth.ts`.

```csharp
public static class OAuth
```

## Public Members

| name | description |
| --- | --- |
| static [AssertPkceSupported](OAuth/AssertPkceSupported.md)(…) | Confirms, from already-discovered authorization-server metadata, that the AS advertises PKCE `S256` support, throwing when it cannot be confirmed so the client refuses to proceed (spec §28.5, R-28.5-k). Call before building an authorization request. |
| static [BuildAuthorizeUrl](OAuth/BuildAuthorizeUrl.md)(…) | Builds the authorization-request URL (spec §23.5): the AS metadata's `authorization_endpoint` with `response_type=code`, the client id, redirect URI, scope, state, PKCE challenge (`code_challenge_method=S256`), and the RFC 8707 `resource` parameter for audience binding (§23.6). Existing query parameters on the endpoint are preserved. |
| static [CreatePkcePair](OAuth/CreatePkcePair.md)() | Generates a PKCE `S256` pair (spec §23.5): a random 32-byte verifier (base64url) and the challenge `base64url(SHA256(verifier))`. Backed by `System.Security.Cryptography` via [`Pkce`](../Stackific.Mcp.Protocol/Pkce.md). |
| static [DiscoverOAuthMetadataAsync](OAuth/DiscoverOAuthMetadataAsync.md)(…) | Discovers protected-resource metadata (RFC 9728) then authorization-server metadata (RFC 8414/OIDC) for an MCP endpoint, walking the full well-known fallback ordering and validating both documents (spec §23.2–§23.3). |
| static [ExchangeAuthorizationCodeAsync](OAuth/ExchangeAuthorizationCodeAsync.md)(…) | Exchanges an authorization code (+ PKCE verifier) for tokens (spec §23.5): POSTs a form-urlencoded body to the AS metadata's `token_endpoint` with `grant_type=authorization_code`, the code, the verifier, the redirect URI, the client id, and the RFC 8707 `resource` parameter so the token is audience-bound to this server (§23.6). Validates the response and that `token_type` is `Bearer` (case-insensitive; R-23.8-b). |
| static [ParseChallenge](OAuth/ParseChallenge.md)(…) | Parses a `401` response's `WWW-Authenticate` header into the structured `Bearer` challenge a client reacts to (spec §23.1, R-23.1-z): the `resource_metadata` URI to fetch and the required `scope`. Returns `null` when the response carried no parseable `Bearer` challenge. |
| static [RefreshTokenAsync](OAuth/RefreshTokenAsync.md)(…) | Refreshes an access token (spec §23.9): POSTs a form-urlencoded body to the AS metadata's `token_endpoint` with `grant_type=refresh_token`, the refresh token, the client id, and the SAME RFC 8707 `resource` parameter so the refreshed token stays audience-bound (R-23.9-e). An OPTIONAL narrowed *scope* MAY be supplied (R-23.9-f). The new refresh token (when issued) is returned on [`RefreshToken`](./OAuth.TokenResponse/RefreshToken.md). |
| static [RegisterClientAsync](OAuth/RegisterClientAsync.md)(…) | Performs dynamic client registration (spec §23.4 / RFC 7591): POSTs to the AS metadata's `registration_endpoint` with the client name, redirect URIs, the `authorization_code` grant, and an `application_type` classified from the redirect URIs (loopback ⇒ `native`). Throws on a hard failure; for retryable-aware handling use [`TryRegisterClientAsync`](./OAuth/TryRegisterClientAsync.md). |
| static [TryRegisterClientAsync](OAuth/TryRegisterClientAsync.md)(…) | Performs dynamic client registration (spec §23.4 / RFC 7591), surfacing a structured result rather than throwing so the client can handle a registration failure and decide whether to retry with an adjusted `application_type` (R-23.4-p, R-23.4-q, R-23.4-r). The body always carries the REQUIRED `application_type`, classified from the redirect URIs. |
| static [VerifyAuthorizationRedirect](OAuth/VerifyAuthorizationRedirect.md)(…) | Verifies the authorization redirect before redeeming the code (spec §23.5/§23.7): the returned *returnedState* MUST equal *sentState* (CSRF defense, R-23.5-l), and the `iss` is validated per the §23.7 decision table — a PRESENT *returnedIss* is ALWAYS compared to *issuer* by exact string match regardless of advertisement (R-23.7-f), and an ABSENT `iss` is rejected when the AS advertises support (R-23.7-e). Throws [`McpError`](../Stackific.Mcp.JsonRpc/McpError.md) (`-32602`) on any mismatch. |
| record [OAuthMetadata](OAuth.OAuthMetadata.md) | The discovered OAuth metadata for a protected MCP resource (spec §23.2–§23.3). |
| record [PkcePair](OAuth.PkcePair.md) | A PKCE verifier/challenge pair (spec §23.5). |
| record [RegisteredClient](OAuth.RegisteredClient.md) | A dynamically registered client (spec §23.4 / RFC 7591). |
| record [TokenResponse](OAuth.TokenResponse.md) | A token-endpoint response (spec §23.5). |

## Remarks

The flow primitives (PKCE, discovery URI ordering, metadata schemas/validators, the `iss` decision table) live in [`Pkce`](../Stackific.Mcp.Protocol/Pkce.md), [`WellKnownDiscovery`](../Stackific.Mcp.Protocol/WellKnownDiscovery.md), [`ProtectedResourceMetadata`](../Stackific.Mcp.Protocol/ProtectedResourceMetadata.md), [`AuthorizationServerMetadata`](../Stackific.Mcp.Protocol/AuthorizationServerMetadata.md), and [`AuthorizationRedirect`](../Stackific.Mcp.Protocol/AuthorizationRedirect.md); this class is the HTTP wiring that drives them with an injectable HttpClient (so the discovery/registration/token calls are offline-testable through a stub HttpMessageHandler).

## See Also

* namespace [Stackific.Mcp.Client](../Stackific.Mcp.md)

<!-- DO NOT EDIT: generated by xmldocmd for Stackific.Mcp.dll -->
