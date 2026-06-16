"""MCP client host (2026-07-28) — the high-level counterpart to the server harness.

It owns the client-side runtime an embedder needs:

* stamps every outgoing request with the REQUIRED per-request ``_meta`` envelope —
  protocol version, client identity, client capabilities (§4.3);
* performs discovery + revision negotiation (``server/discover``, §5.3–§5.4), caching
  only the negotiated revision + status (the connection carries no conversational
  state, §4.4/§7.6);
* exposes convenience methods for the read/call feature methods, surfacing a delivered
  JSON-RPC error as a :class:`RequestError`.

Scope of this port: the request/response client over a :class:`ClientTransport`.
Deferred to their own phases (each clearly out of scope here): inbound server→client
requests (sampling/elicitation/roots, §20–§21), subscriptions (§10), correlated
progress + cancellation (§15), OAuth, and retry.
"""

from __future__ import annotations

from mcp.client.transport import ClientTransport
from mcp.protocol.discovery import resolve_instructions, select_revision
from mcp.protocol.meta import (
  CLIENT_CAPABILITIES_META_KEY,
  CLIENT_INFO_META_KEY,
  CURRENT_PROTOCOL_VERSION,
  PROTOCOL_VERSION_META_KEY,
)


class RequestError(Exception):
  """A delivered JSON-RPC error response surfaced as a raised error.

  Distinct from a transport channel failure: the request was fully delivered and the
  peer answered with an ``error``. (§7.5)
  """

  def __init__(self, code: int | None, message: str, data: object = None) -> None:
    super().__init__(message)
    self.code = code
    self.data = data


class Client:
  """A stateless MCP client host driving a server over a :class:`ClientTransport`."""

  def __init__(
    self,
    transport: ClientTransport,
    client_info: dict,
    *,
    capabilities: dict | None = None,
    protocol_versions: list[str] | None = None,
  ) -> None:
    self._transport = transport
    #: This client's ``Implementation`` identity, stamped into every request (§4.3).
    self.client_info = client_info
    #: Capabilities declared in every request's ``_meta`` (§6.2).
    self.capabilities = capabilities or {}
    #: Acceptable revisions, most-preferred first.
    self.preferred_versions = protocol_versions or [CURRENT_PROTOCOL_VERSION]
    #: The revision negotiated via discovery; ``None`` until :meth:`discover` runs.
    self.negotiated_version: str | None = None
    self.server_info: dict | None = None
    self.server_capabilities: dict | None = None
    self.instructions: str | None = None
    self._id = 0

  # ── envelope + core request ──
  def _meta(self) -> dict:
    """Build the REQUIRED per-request ``_meta`` envelope for this request (§4.3)."""
    version = self.negotiated_version or self.preferred_versions[0]
    return {
      PROTOCOL_VERSION_META_KEY: version,
      CLIENT_INFO_META_KEY: self.client_info,
      CLIENT_CAPABILITIES_META_KEY: self.capabilities,
    }

  def request(self, method: str, params: dict | None = None) -> dict:
    """Send a request and return its ``result``, or raise :class:`RequestError`.

    The ``_meta`` envelope is merged into ``params`` automatically.
    """
    self._id += 1
    message = {
      "jsonrpc": "2.0",
      "id": self._id,
      "method": method,
      "params": {**(params or {}), "_meta": self._meta()},
    }
    response = self._transport.request(message)
    if "error" in response:
      error = response["error"]
      raise RequestError(error.get("code"), error.get("message", ""), error.get("data"))
    return response.get("result", {})

  # ── discovery ──
  def discover(self) -> dict:
    """Run ``server/discover``, cache status, and negotiate the protocol revision (§5.3)."""
    result = self.request("server/discover")
    self.server_info = result.get("serverInfo")
    self.server_capabilities = result.get("capabilities")
    self.instructions = resolve_instructions(result)
    self.negotiated_version = select_revision(result.get("supportedVersions", []), self.preferred_versions)
    return result

  @property
  def connected(self) -> bool:
    """``True`` once discovery has negotiated a shared revision."""
    return self.negotiated_version is not None

  def status(self) -> dict:
    """A snapshot of connection status (mirrors the companion's BackendStatus shape)."""
    return {
      "connected": self.connected,
      "negotiatedVersion": self.negotiated_version,
      "serverInfo": self.server_info,
      "serverCapabilities": self.server_capabilities,
      "instructions": self.instructions,
    }

  # ── convenience feature methods ──
  def ping(self) -> dict:
    return self.request("ping")

  def list_tools(self, cursor: str | None = None) -> dict:
    return self.request("tools/list", {"cursor": cursor} if cursor else {})

  def call_tool(self, name: str, arguments: dict | None = None) -> dict:
    return self.request("tools/call", {"name": name, "arguments": arguments or {}})

  def list_resources(self, cursor: str | None = None) -> dict:
    return self.request("resources/list", {"cursor": cursor} if cursor else {})

  def list_resource_templates(self, cursor: str | None = None) -> dict:
    return self.request("resources/templates/list", {"cursor": cursor} if cursor else {})

  def read_resource(self, uri: str) -> dict:
    return self.request("resources/read", {"uri": uri})

  def list_prompts(self, cursor: str | None = None) -> dict:
    return self.request("prompts/list", {"cursor": cursor} if cursor else {})

  def get_prompt(self, name: str, arguments: dict | None = None) -> dict:
    return self.request("prompts/get", {"name": name, "arguments": arguments or {}})

  def complete(self, ref: dict, argument: dict, context: dict | None = None) -> dict:
    params = {"ref": ref, "argument": argument}
    if context is not None:
      params["context"] = context
    return self.request("completion/complete", params)

  def raw(self, method: str, params: dict | None = None) -> dict:
    """Escape hatch: issue an arbitrary method with the ``_meta`` envelope applied."""
    return self.request(method, params)
