"""Python MCP reference server (FastAPI) built on the py-sdk (``stackific-mcp``).

Serves stateless Streamable HTTP (protocol 2026-07-28) on ``/mcp`` via the SDK's
:func:`mcp.server.create_mcp_request_handler`. This is the real Python counterpart to
``ts-mcp-server``: it registers a few tools, a resource, a resource template, and a
prompt, and answers ``server/discover`` + every feature method the SDK supports.
"""

import os

import uvicorn
from fastapi import FastAPI, Request, Response

from mcp.server import McpServer, ToolContext, create_mcp_request_handler

# Port is owned by the root Taskfile; this default matches it for standalone runs.
PORT = int(os.environ.get("PY_MCP_SERVER_PORT", "8101"))


def build_server() -> McpServer:
  """Construct the reference server with a small set of demo features."""
  server = McpServer(
    {"name": "py-mcp-server", "title": "Python MCP Server", "version": "0.1.0"},
    {
      "tools": {"listChanged": True},
      "resources": {"listChanged": True},
      "prompts": {"listChanged": True},
      "completions": {},
    },
  )

  def echo(args: dict, ctx: ToolContext) -> dict:
    return {"content": [{"type": "text", "text": str(args.get("text", ""))}]}

  server.register_tool(
    "echo",
    echo,
    description="Echo back the provided text.",
    input_schema={"type": "object", "properties": {"text": {"type": "string"}}, "required": ["text"]},
  )

  def add(args: dict, ctx: ToolContext) -> dict:
    total = (args.get("a") or 0) + (args.get("b") or 0)
    return {"content": [{"type": "text", "text": f"{args.get('a')} + {args.get('b')} = {total}"}], "structuredContent": {"sum": total}}

  server.register_tool(
    "add",
    add,
    description="Add two numbers.",
    input_schema={
      "type": "object",
      "properties": {"a": {"type": "number"}, "b": {"type": "number"}},
      "required": ["a", "b"],
    },
    output_schema={"type": "object", "properties": {"sum": {"type": "number"}}},
  )

  server.register_resource(
    "readme",
    "file:///readme.md",
    lambda uri: {"contents": [{"uri": uri, "mimeType": "text/markdown", "text": "# Python MCP Server\nServed by py-sdk."}]},
    description="A sample readme resource.",
    mime_type="text/markdown",
  )

  server.register_resource_template(
    "greeting",
    "greet://{name}",
    lambda uri, variables: {"contents": [{"uri": uri, "text": f"Hello, {variables['name']}!"}]},
    description="A templated greeting resource.",
  )

  server.register_prompt(
    "greet",
    lambda args: {"messages": [{"role": "user", "content": {"type": "text", "text": f"Say hello to {args.get('name', 'world')}."}}]},
    description="Generate a greeting prompt.",
    arguments=[
      {
        "name": "name",
        "description": "Who to greet",
        "required": True,
        "complete": lambda v: [n for n in ("Ada", "Alan", "Grace") if n.lower().startswith(v.lower())],
      }
    ],
  )
  return server


server = build_server()
handler = create_mcp_request_handler(server)

app = FastAPI(title="py-mcp-server")


@app.get("/health")
def health() -> dict[str, str]:
  return {
    "status": "ok",
    "name": "py-mcp-server",
    "language": "python",
    "framework": "fastapi",
    "sdk": "stackific-mcp",
    "protocol": "2026-07-28",
    "transport": "streamable-http",
  }


@app.api_route("/mcp", methods=["GET", "POST", "DELETE", "OPTIONS"])
async def mcp(request: Request) -> Response:
  """Hand the HTTP request to the SDK's Streamable HTTP handler and relay its response."""
  body = await request.body()
  result = handler(request.method, "/mcp", dict(request.headers), body)
  return Response(content=result.body, status_code=result.status, headers=result.headers)


if __name__ == "__main__":
  uvicorn.run("main:app", host="127.0.0.1", port=PORT, reload=True)
