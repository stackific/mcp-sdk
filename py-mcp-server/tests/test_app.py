"""Tests for the real py-mcp-server FastAPI app, driven through the SDK Client over the
FastAPI route (TestClient transport) — exercising the full wire path.
"""

from fastapi.testclient import TestClient

from mcp.client import Client
from mcp.client.transport import ClientTransport
from mcp.protocol.discovery import is_discover_result

from main import app

tc = TestClient(app)


class _TestClientTransport(ClientTransport):
  """Bridges the SDK Client to the FastAPI app's /mcp route via Starlette's TestClient."""

  def request(self, message: dict) -> dict:
    return tc.post("/mcp", json=message).json()


def make_client() -> Client:
  return Client(_TestClientTransport(), {"name": "t", "version": "1"}, capabilities={"tools": {}})


def test_health():
  resp = tc.get("/health")
  assert resp.status_code == 200
  assert resp.json()["sdk"] == "stackific-mcp"


def test_discover():
  c = make_client()
  result = c.discover()
  assert is_discover_result(result)
  assert result["serverInfo"]["name"] == "py-mcp-server"
  assert c.negotiated_version == "2026-07-28"


def test_list_tools():
  c = make_client()
  names = {t["name"] for t in c.list_tools()["tools"]}
  assert {"echo", "add"} <= names


def test_call_tool_echo():
  c = make_client()
  result = c.call_tool("echo", {"text": "hello"})
  assert result["content"][0]["text"] == "hello"


def test_call_tool_add_structured():
  c = make_client()
  result = c.call_tool("add", {"a": 2, "b": 3})
  assert result["structuredContent"]["sum"] == 5


def test_read_resource():
  c = make_client()
  result = c.read_resource("file:///readme.md")
  assert result["contents"][0]["mimeType"] == "text/markdown"


def test_resource_template_read():
  c = make_client()
  result = c.read_resource("greet://World")
  assert "Hello, World!" in result["contents"][0]["text"]


def test_prompt_and_completion():
  c = make_client()
  got = c.get_prompt("greet", {"name": "Ada"})
  assert "Ada" in got["messages"][0]["content"]["text"]
  completion = c.complete({"type": "ref/prompt", "name": "greet"}, {"name": "name", "value": "A"})
  assert "Ada" in completion["completion"]["values"]
