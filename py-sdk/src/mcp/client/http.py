"""Streamable HTTP client transport (MCP 2026-07-28, §9) — stateless request/response.

POSTs one JSON-RPC request as ``application/json`` and returns the parsed response. It
mirrors the request's ``_meta`` protocol revision into the ``MCP-Protocol-Version``
header (§5.2). Built on the standard library (``urllib``) so the SDK core stays
dependency-free; a 4xx carrying a JSON-RPC error body is returned as the response (the
:class:`~mcp.client.client.Client` raises ``RequestError`` from it), while a true
channel failure raises :class:`ClientTransportError`.

Deferred (own phases): the ``text/event-stream`` response path, OAuth, and retry.
"""

from __future__ import annotations

import json
import urllib.error
import urllib.request

from mcp.client.transport import ClientTransport, ClientTransportError
from mcp.protocol.meta import CURRENT_PROTOCOL_VERSION, PROTOCOL_VERSION_META_KEY


class StreamableHttpClientTransport(ClientTransport):
  """A stateless Streamable HTTP client transport targeting a single MCP endpoint URL."""

  def __init__(
    self,
    url: str,
    *,
    protocol_version: str = CURRENT_PROTOCOL_VERSION,
    timeout: float = 30.0,
  ) -> None:
    self._url = url
    self._protocol_version = protocol_version
    self._timeout = timeout

  def request(self, message: dict) -> dict:
    data = json.dumps(message).encode("utf-8")
    meta = (message.get("params") or {}).get("_meta") or {}
    version = meta.get(PROTOCOL_VERSION_META_KEY, self._protocol_version)
    http_request = urllib.request.Request(
      self._url,
      data=data,
      method="POST",
      headers={"Content-Type": "application/json", "MCP-Protocol-Version": version},
    )

    try:
      with urllib.request.urlopen(http_request, timeout=self._timeout) as resp:
        body = resp.read()
    except urllib.error.HTTPError as exc:
      # A 4xx/5xx may still carry a JSON-RPC error body (e.g. -32001 on 400); read it.
      body = exc.read()
    except urllib.error.URLError as exc:
      raise ClientTransportError(f"transport failure contacting {self._url}: {exc}") from exc

    try:
      return json.loads(body.decode("utf-8"))
    except ValueError as exc:
      raise ClientTransportError("server returned a non-JSON response") from exc
