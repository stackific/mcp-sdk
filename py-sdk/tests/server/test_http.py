"""Tests for the Streamable HTTP server handler (§9, §22.6)."""

import json

from mcp.protocol.discovery import build_discover_request, is_discover_result
from mcp.protocol.errors import HEADER_MISMATCH_CODE, METHOD_NOT_FOUND_CODE, PARSE_ERROR_CODE
from mcp.protocol.meta import PROTOCOL_VERSION_META_KEY
from mcp.server.http import create_mcp_request_handler
from mcp.server.server import McpServer

INFO = {"name": "srv", "version": "1.0"}
CLIENT = {"name": "cli", "version": "0.1"}


def handler():
  return create_mcp_request_handler(McpServer(INFO, {"tools": {}}))


def post(h, body, headers=None, path="/mcp"):
  return h("POST", path, headers or {}, json.dumps(body) if not isinstance(body, str) else body)


class TestPost:
  def test_request_returns_200_json(self):
    h = handler()
    resp = post(h, build_discover_request(1, "2026-07-28", CLIENT, {}))
    assert resp.status == 200
    assert resp.headers["Content-Type"] == "application/json"
    payload = json.loads(resp.body)
    assert payload["id"] == 1 and is_discover_result(payload["result"])

  def test_cors_headers_present(self):
    resp = post(handler(), build_discover_request(1, "2026-07-28", CLIENT, {}))
    assert resp.headers["Access-Control-Allow-Origin"] == "*"

  def test_notification_returns_202(self):
    resp = post(handler(), {"jsonrpc": "2.0", "method": "notifications/foo"})
    assert resp.status == 202 and resp.body == ""

  def test_malformed_json_returns_400_parse_error(self):
    resp = post(handler(), "{not json")
    assert resp.status == 400
    assert json.loads(resp.body)["error"]["code"] == PARSE_ERROR_CODE

  def test_bytes_body_supported(self):
    h = handler()
    raw = json.dumps(build_discover_request(1, "2026-07-28", CLIENT, {})).encode("utf-8")
    resp = h("POST", "/mcp", {}, raw)
    assert resp.status == 200

  def test_method_not_found_rides_on_200(self):
    h = handler()
    req = {"jsonrpc": "2.0", "id": 5, "method": "bogus"}
    resp = post(h, req)
    assert resp.status == 200
    assert json.loads(resp.body)["error"]["code"] == METHOD_NOT_FOUND_CODE


class TestHeaderMismatch:
  def test_matching_header_ok(self):
    h = handler()
    req = build_discover_request(1, "2026-07-28", CLIENT, {})
    resp = post(h, req, headers={"MCP-Protocol-Version": "2026-07-28"})
    assert resp.status == 200

  def test_mismatched_header_is_400(self):
    h = handler()
    req = build_discover_request(1, "2026-07-28", CLIENT, {})
    resp = post(h, req, headers={"MCP-Protocol-Version": "2025-01-01"})
    assert resp.status == 400
    assert json.loads(resp.body)["error"]["code"] == HEADER_MISMATCH_CODE

  def test_absent_header_not_enforced(self):
    h = handler()
    req = build_discover_request(1, "2026-07-28", CLIENT, {})
    resp = post(h, req)  # no MCP-Protocol-Version header
    assert resp.status == 200


class TestRouting:
  def test_wrong_path_404(self):
    assert handler()("POST", "/other", {}, "{}").status == 404

  def test_options_preflight_204(self):
    resp = handler()("OPTIONS", "/mcp", {}, "")
    assert resp.status == 204
    assert resp.headers["Access-Control-Allow-Origin"] == "*"

  def test_delete_is_noop_200(self):
    assert handler()("DELETE", "/mcp", {}, "").status == 200

  def test_get_405(self):
    assert handler()("GET", "/mcp", {}, "").status == 405

  def test_cors_can_be_disabled(self):
    h = create_mcp_request_handler(McpServer(INFO, {"tools": {}}), cors=None)
    resp = h("OPTIONS", "/mcp", {}, "")
    assert "Access-Control-Allow-Origin" not in resp.headers
