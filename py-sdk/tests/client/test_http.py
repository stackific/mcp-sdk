"""End-to-end test: the Client's Streamable HTTP transport against the server's HTTP
handler, over a real socket (a threaded http.server). Validates both halves together.
"""

import threading
from http.server import BaseHTTPRequestHandler, HTTPServer

import pytest

from mcp.client.client import Client, RequestError
from mcp.client.http import StreamableHttpClientTransport
from mcp.protocol.discovery import is_discover_result
from mcp.server.http import create_mcp_request_handler
from mcp.server.server import McpServer

INFO = {"name": "srv", "version": "1.0"}
CLIENT = {"name": "cli", "version": "0.1"}


def _make_server():
  server = McpServer(INFO, {"tools": {}})
  server.register_tool("echo", lambda args, c: {"content": [{"type": "text", "text": args.get("msg", "")}]})
  mcp_handler = create_mcp_request_handler(server)

  class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):  # silence test server logging
      pass

    def do_POST(self):
      length = int(self.headers.get("Content-Length", 0))
      body = self.rfile.read(length)
      resp = mcp_handler("POST", self.path, dict(self.headers), body)
      payload = resp.body.encode("utf-8")
      self.send_response(resp.status)
      for key, value in resp.headers.items():
        self.send_header(key, value)
      self.send_header("Content-Length", str(len(payload)))
      self.end_headers()
      if payload:
        self.wfile.write(payload)

  httpd = HTTPServer(("127.0.0.1", 0), Handler)
  thread = threading.Thread(target=httpd.serve_forever, daemon=True)
  thread.start()
  return httpd


@pytest.fixture
def base_url():
  httpd = _make_server()
  host, port = httpd.server_address
  try:
    yield f"http://{host}:{port}/mcp"
  finally:
    httpd.shutdown()


def _client(base_url: str) -> Client:
  return Client(StreamableHttpClientTransport(base_url), CLIENT, capabilities={"tools": {}})


class TestOverHttp:
  def test_discover(self, base_url):
    c = _client(base_url)
    result = c.discover()
    assert is_discover_result(result)
    assert c.negotiated_version == "2026-07-28"
    assert c.server_info == INFO

  def test_list_and_call_tool(self, base_url):
    c = _client(base_url)
    c.discover()
    assert c.list_tools()["tools"][0]["name"] == "echo"
    assert c.call_tool("echo", {"msg": "over-http"})["content"][0]["text"] == "over-http"

  def test_request_error_propagates_over_http(self, base_url):
    c = _client(base_url)
    with pytest.raises(RequestError) as exc:
      c.raw("bogus/method")
    assert exc.value.code == -32601
