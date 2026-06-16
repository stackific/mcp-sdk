"""MCP client host: the high-level :class:`Client`, its transports, and OAuth helpers.

Re-exports the public surface of :mod:`stackific.mcp.client.client`, :mod:`stackific.mcp.client.transport`,
:mod:`stackific.mcp.client.http`, and :mod:`stackific.mcp.client.oauth`.
"""

from stackific.mcp.client.client import Client, RequestError
from stackific.mcp.client.http import StreamableHttpClientTransport, SubscriptionStream
from stackific.mcp.client.oauth import (
  build_authorize_url,
  create_pkce_pair,
  discover_oauth_metadata,
  exchange_authorization_code,
  register_client,
  verify_authorization_redirect,
)
from stackific.mcp.client.transport import ClientTransport, ClientTransportError

__all__ = [
  "Client",
  "RequestError",
  "ClientTransport",
  "ClientTransportError",
  "StreamableHttpClientTransport",
  "SubscriptionStream",
  "create_pkce_pair",
  "discover_oauth_metadata",
  "register_client",
  "build_authorize_url",
  "exchange_authorization_code",
  "verify_authorization_redirect",
]
