"""Tests for the Client over an in-process direct transport (server bridged via
process_message), exercising discovery, the _meta envelope, and feature methods.
"""

import pytest

from mcp.client.client import Client, RequestError
from mcp.client.transport import ClientTransport
from mcp.protocol.meta import PROTOCOL_VERSION_META_KEY
from mcp.server.runtime import process_message
from mcp.server.server import McpServer

INFO = {"name": "srv", "version": "1.0"}
CLIENT = {"name": "cli", "version": "0.1"}
CAPS = {"tools": {}, "resources": {}, "prompts": {}}


class DirectClientTransport(ClientTransport):
  """In-process bridge: hand each request straight to a server via process_message."""

  def __init__(self, server: McpServer) -> None:
    self._server = server

  def request(self, message: dict) -> dict:
    response = process_message(self._server, message)
    assert response is not None  # a request always yields a response
    return response


def build_server() -> McpServer:
  s = McpServer(INFO, CAPS)
  s.register_tool("echo", lambda args, c: {"content": [{"type": "text", "text": args.get("msg", "")}]})
  # A tool that echoes the per-request _meta so we can assert the envelope propagated.
  s.register_tool("meta", lambda args, c: {"structuredContent": dict(c.meta)})
  return s


def build_client() -> Client:
  return Client(DirectClientTransport(build_server()), CLIENT, capabilities={"tools": {}})


class TestDiscovery:
  def test_discover_negotiates_and_caches(self):
    c = build_client()
    result = c.discover()
    assert result["serverInfo"] == INFO
    assert c.negotiated_version == "2026-07-28"
    assert c.connected
    assert c.server_capabilities == CAPS

  def test_status_snapshot(self):
    c = build_client()
    assert c.status()["connected"] is False  # before discover
    c.discover()
    status = c.status()
    assert status["connected"] and status["negotiatedVersion"] == "2026-07-28"


class TestEnvelope:
  def test_meta_envelope_reaches_server(self):
    c = build_client()
    result = c.call_tool("meta")
    meta = result["structuredContent"]
    assert meta[PROTOCOL_VERSION_META_KEY] == "2026-07-28"
    assert "io.modelcontextprotocol/clientInfo" in meta
    assert "io.modelcontextprotocol/clientCapabilities" in meta


class TestFeatureMethods:
  def test_ping(self):
    assert build_client().ping() == {}

  def test_list_and_call_tool(self):
    c = build_client()
    tools = c.list_tools()
    names = {t["name"] for t in tools["tools"]}
    assert {"echo", "meta"} <= names
    assert c.call_tool("echo", {"msg": "hi"})["content"][0]["text"] == "hi"

  def test_request_error_on_unknown_method(self):
    c = build_client()
    with pytest.raises(RequestError) as exc:
      c.raw("does/not/exist")
    assert exc.value.code == -32601

  def test_request_error_on_unknown_tool(self):
    c = build_client()
    with pytest.raises(RequestError) as exc:
      c.call_tool("nope")
    assert exc.value.code == -32602
