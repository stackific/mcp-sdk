"""Python MCP client host (FastAPI) built on the py-sdk (``stackific-mcp``).

Hosts an MCP :class:`~mcp.client.Client` connected to ``py-mcp-server`` over Streamable
HTTP and exposes the companion frontend's REST + Server-Sent-Events surface. This is the
real Python counterpart to ``ts-mcp-client``: capability calls drive genuine MCP
requests and return the server's results.

Deferred (own phases, like the SDK seams they rely on): live per-frame wire tapping on
``/debug/stream`` (a synthesized status + note is emitted instead), server→client
features (sampling/elicitation/roots), subscriptions, and tasks — the catch-all returns
a friendly not-implemented for those.
"""

import asyncio
import json
import os
import time
from collections.abc import Callable

import uvicorn
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, StreamingResponse

from mcp.client import Client, RequestError, StreamableHttpClientTransport

# Ports + the server URL are owned by the root Taskfile; these defaults match it.
PORT = int(os.environ.get("PY_MCP_CLIENT_PORT", "8102"))
MCP_SERVER_BASE = os.environ.get("PY_MCP_SERVER_URL", "http://localhost:8101").rstrip("/")
MCP_ENDPOINT = f"{MCP_SERVER_BASE}/mcp"

client = Client(
  StreamableHttpClientTransport(MCP_ENDPOINT),
  {"name": "py-mcp-client", "title": "Python MCP Client", "version": "0.1.0"},
  capabilities={},
)

app = FastAPI(title="py-mcp-client")
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])


def ensure_connected() -> None:
  """Lazily run discovery so status reflects the real connection. Failures are swallowed
  here and surfaced via ``status.connected == False``.
  """
  if not client.connected:
    try:
      client.discover()
    except Exception:  # noqa: BLE001 — unreachable server → connected stays False
      pass


def run(fn: Callable[[], object]) -> dict:
  """Shape a capability call into the frontend's ApiResult ({ok, result} / {ok, error})."""
  try:
    return {"ok": True, "result": fn()}
  except RequestError as exc:
    return {"ok": False, "error": {"message": str(exc), "code": exc.code, "data": exc.data}}
  except Exception as exc:  # noqa: BLE001 — transport/other failure → uniform error shape
    return {"ok": False, "error": {"message": str(exc)}}


def _status() -> dict:
  return {**client.status(), "serverUrl": MCP_ENDPOINT}


@app.get("/health")
def health() -> dict[str, str]:
  return {"status": "ok", "language": "python", "framework": "fastapi", "sdk": "stackific-mcp"}


@app.get("/info")
def info() -> dict:
  ensure_connected()
  return {"name": "py-mcp-client", "language": "python", "serverUrl": MCP_ENDPOINT, "status": _status()}


@app.get("/api/status")
def api_status() -> dict:
  ensure_connected()
  return _status()


@app.post("/api/connect")
def api_connect() -> dict:
  # Always (re)discover — drives a visible server/discover round-trip.
  return run(lambda: (client.discover(), _status())[1])


@app.get("/api/discover")
def api_discover() -> dict:
  return run(client.discover)


@app.get("/api/tools")
def api_tools() -> dict:
  ensure_connected()
  return run(client.list_tools)


@app.post("/api/tools/call")
async def api_tools_call(request: Request) -> dict:
  body = await request.json()
  ensure_connected()
  return run(lambda: client.call_tool(body.get("name"), body.get("arguments") or {}))


@app.get("/api/resources")
def api_resources() -> dict:
  ensure_connected()
  return run(client.list_resources)


@app.get("/api/resource-templates")
def api_resource_templates() -> dict:
  ensure_connected()
  return run(client.list_resource_templates)


@app.post("/api/resources/read")
async def api_resources_read(request: Request) -> dict:
  body = await request.json()
  ensure_connected()
  return run(lambda: client.read_resource(body.get("uri")))


@app.get("/api/prompts")
def api_prompts() -> dict:
  ensure_connected()
  return run(client.list_prompts)


@app.post("/api/prompts/get")
async def api_prompts_get(request: Request) -> dict:
  body = await request.json()
  ensure_connected()
  return run(lambda: client.get_prompt(body.get("name"), body.get("arguments") or {}))


@app.post("/api/complete")
async def api_complete(request: Request) -> dict:
  body = await request.json()
  ensure_connected()
  return run(lambda: client.complete(body.get("ref"), body.get("argument"), body.get("context")))


@app.post("/api/raw")
async def api_raw(request: Request) -> dict:
  body = await request.json()
  ensure_connected()
  return run(lambda: client.raw(body.get("method"), body.get("params") or {}))


@app.get("/api/roots")
def api_roots() -> dict:
  return {"roots": []}  # the client roots feature is deferred


@app.get("/debug/stream")
async def debug_stream() -> StreamingResponse:
  """SSE relay matching the TS backend. Emits the live connection status + a note, then
  keep-alive pings. Per-frame wire tapping is a deferred enhancement.
  """

  def sse(event: str, data: dict) -> str:
    return f"event: {event}\ndata: {json.dumps(data)}\n\n"

  async def events():
    await asyncio.to_thread(ensure_connected)
    reached = "connected to " + MCP_ENDPOINT if client.connected else "could not reach " + MCP_ENDPOINT
    yield sse("status", _status())
    yield sse(
      "frame",
      {
        "seq": 1,
        "ts": int(time.time() * 1000),
        "dir": "local",
        "kind": "note",
        "summary": f"Python stack live via py-sdk — client {reached}.",
      },
    )
    while True:
      await asyncio.sleep(15)
      yield sse("ping", {})

  return StreamingResponse(
    events(),
    media_type="text/event-stream",
    headers={"Cache-Control": "no-cache", "Connection": "keep-alive"},
  )


# Catch-all for capabilities not implemented in this host yet (tasks, subscriptions,
# authorization, transport probe, …). Registered last so the specific routes win.
@app.api_route("/api/{path:path}", methods=["GET", "POST"])
def api_not_implemented(path: str) -> JSONResponse:
  return JSONResponse(
    {"ok": False, "error": {"message": f"'/api/{path}' is not implemented in the Python client host yet."}}
  )


if __name__ == "__main__":
  uvicorn.run("main:app", host="127.0.0.1", port=PORT, reload=True)
