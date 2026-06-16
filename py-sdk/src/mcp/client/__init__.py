"""MCP client host: the high-level :class:`Client` and its transports.

Re-exports the public surface of :mod:`mcp.client.client`, :mod:`mcp.client.transport`,
and :mod:`mcp.client.http`.
"""

from mcp.client.client import Client, RequestError
from mcp.client.http import StreamableHttpClientTransport
from mcp.client.transport import ClientTransport, ClientTransportError

__all__ = [
  "Client",
  "RequestError",
  "ClientTransport",
  "ClientTransportError",
  "StreamableHttpClientTransport",
]
