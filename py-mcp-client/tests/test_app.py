"""Tests for the py-mcp-client FastAPI app.

These run with no upstream py-mcp-server, so they assert graceful behaviour when the
server is unreachable (status not connected, capability calls return an error shape, no
crash) plus the static endpoints. The happy path against a live server is covered by the
end-to-end verification in the Taskfile phase.
"""

from fastapi.testclient import TestClient

from main import app

tc = TestClient(app)


def test_health():
  resp = tc.get("/health")
  assert resp.status_code == 200
  assert resp.json()["sdk"] == "stackific-mcp"


def test_status_when_server_unreachable():
  body = tc.get("/api/status").json()
  assert body["connected"] is False
  assert body["serverUrl"].endswith("/mcp")


def test_capability_call_returns_error_shape_not_crash():
  body = tc.get("/api/tools").json()
  assert body["ok"] is False
  assert "error" in body


def test_connect_reports_failure_gracefully():
  body = tc.post("/api/connect").json()
  assert body["ok"] is False


def test_roots_endpoint():
  assert tc.get("/api/roots").json() == {"roots": []}


def test_unimplemented_capability_catch_all():
  body = tc.post("/api/tasks/create", json={}).json()
  assert body["ok"] is False
  assert "not implemented" in body["error"]["message"]
