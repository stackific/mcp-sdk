"""Tools II — calling, errors, annotations & change notifications (§16.5–§16.9).

The runtime half of MCP tools on top of :mod:`mcp.protocol.tools`. Defines the
``tools/call`` request (incl. the multi-round-trip retry fields), the ``CallToolResult``
(with the ``isError`` model), the two-layer error split (a tool-execution failure is a
*successful* result with ``isError: true``; a dispatch failure is a JSON-RPC ``-32602``),
the ``dispatchToolCall`` decision, tool annotations, and the list-changed notification.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field

from mcp.protocol.errors import INVALID_PARAMS_CODE
from mcp.protocol.tools import TOOLS_CALL_METHOD, TOOLS_LIST_CHANGED_METHOD, validate_tool_arguments
from mcp.types.content import is_valid_content_block

__all__ = [
  "TOOLS_CALL_METHOD",
  "TOOLS_LIST_CHANGED_METHOD",
  "INVALID_PARAMS_CODE",
  "is_call_tool_request",
  "resolve_call_tool_arguments",
  "CallToolRequestConfig",
  "build_call_tool_request",
  "CallToolRetryConfig",
  "build_call_tool_retry_request",
  "is_call_tool_result",
  "is_call_tool_error",
  "is_structured_content_present",
  "structured_content_text_fallback",
  "CallToolResultConfig",
  "build_call_tool_result",
  "build_output_schema_result",
  "build_tool_execution_error",
  "build_unknown_tool_error",
  "build_invalid_arguments_error",
  "ToolDispatch",
  "dispatch_tool_call",
  "TOOL_ANNOTATION_DEFAULTS",
  "resolve_tool_annotation_hints",
  "may_trust_tool_annotations",
  "is_tool_list_changed_notification",
  "build_tool_list_changed_notification",
  "react_to_tool_list_changed",
]

# Sentinel so an explicitly-passed structuredContent=None survives (vs "omitted").
_OMITTED = object()


# ─── §16.5 tools/call request ─────────────────────────────────────────────────

def is_call_tool_request(value: object) -> bool:
  """Return ``True`` for a well-formed ``tools/call`` request: envelope + ``params.name``
  (string). (§16.5, R-16.5-a)
  """
  if not isinstance(value, dict) or value.get("jsonrpc") != "2.0" or value.get("method") != TOOLS_CALL_METHOD:
    return False
  if "id" not in value:
    return False
  params = value.get("params")
  return isinstance(params, dict) and isinstance(params.get("name"), str)


def resolve_call_tool_arguments(params: dict) -> dict:
  """Resolve the effective arguments: the supplied object, or ``{}`` when omitted.
  A server MUST treat omitted ``arguments`` as ``{}``. (§16.5, R-16.5-e)
  """
  args = params.get("arguments")
  return args if isinstance(args, dict) else {}


@dataclass(frozen=True)
class CallToolRequestConfig:
  """Caller-supplied inputs to a first-issue ``tools/call``."""

  name: str
  arguments: dict | None = None
  meta: dict | None = None


def build_call_tool_request(id_: str | int, config: CallToolRequestConfig) -> dict:
  """Build a first-issue ``tools/call`` request; ``arguments``/``_meta`` only when supplied
  (the server applies the omitted-arguments default). (§16.5)
  """
  params: dict = {"name": config.name}
  if config.arguments is not None:
    params["arguments"] = config.arguments
  if config.meta is not None:
    params["_meta"] = config.meta
  return {"jsonrpc": "2.0", "id": id_, "method": TOOLS_CALL_METHOD, "params": params}


@dataclass(frozen=True)
class CallToolRetryConfig:
  """Caller-supplied inputs to a retry of a previously ``input_required`` call."""

  name: str
  input_responses: dict
  request_state: str | None = None
  meta: dict | None = None


def build_call_tool_retry_request(initial_id: str | int, retry_id: str | int, config: CallToolRetryConfig) -> dict:
  """Build a retry ``tools/call`` after an ``input_required`` result, echoing
  ``request_state`` verbatim and supplying ``inputResponses``. (§16.5, S17)

  :raises ValueError: when ``retry_id`` equals ``initial_id`` — a retry MUST use a fresh
    JSON-RPC id. (R-16.5-u)
  """
  if retry_id == initial_id:
    raise ValueError("A tools/call retry MUST use a JSON-RPC id different from the initial request (R-16.5-u)")
  params: dict = {"name": config.name, "inputResponses": config.input_responses}
  if config.request_state is not None:
    params["requestState"] = config.request_state
  if config.meta is not None:
    params["_meta"] = config.meta
  return {"jsonrpc": "2.0", "id": retry_id, "method": TOOLS_CALL_METHOD, "params": params}


# ─── §16.5 CallToolResult ─────────────────────────────────────────────────────

def is_call_tool_result(value: object) -> bool:
  """Return ``True`` for a well-formed (completed) ``CallToolResult`` (§16.5): ``resultType``
  ``"complete"``, a ``content`` list of valid blocks, OPTIONAL ``isError`` bool, ``_meta``.
  """
  if not isinstance(value, dict) or value.get("resultType") != "complete":
    return False
  content = value.get("content")
  if not isinstance(content, list) or not all(is_valid_content_block(b) for b in content):
    return False
  if "isError" in value and not isinstance(value["isError"], bool):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


def is_call_tool_error(result: dict) -> bool:
  """Return ``True`` when the result ended in a tool-execution error (``isError == True``;
  absent ⇒ success). (§16.5/§16.6, R-16.5-q, R-16.6-b)
  """
  return result.get("isError") is True


def is_structured_content_present(result: dict) -> bool:
  """Return ``True`` when ``structuredContent`` is present (an explicit ``null`` counts;
  an omitted key does not). (R-16.5-n)
  """
  return "structuredContent" in result


def structured_content_text_fallback(structured_content: object) -> dict:
  """Serialize a structured value to a ``text`` content block — the SHOULD fallback for
  clients that do not consume structured content. (§16.5, R-16.5-p)
  """
  return {"type": "text", "text": json.dumps(structured_content)}


@dataclass(frozen=True)
class CallToolResultConfig:
  """Server-supplied inputs to a (non-error) ``CallToolResult``.

  Leave ``structured_content`` as the default sentinel to omit it; pass any value
  (including ``None``) to include it.
  """

  content: list
  structured_content: object = _OMITTED
  is_error: bool | None = None
  meta: dict | None = None


def build_call_tool_result(config: CallToolResultConfig) -> dict:
  """Build a completed ``CallToolResult`` (``resultType: "complete"``); optional fields only
  when supplied (an explicit ``structured_content=None`` survives). (§16.5)
  """
  result: dict = {"resultType": "complete", "content": list(config.content)}
  if config.structured_content is not _OMITTED:
    result["structuredContent"] = config.structured_content
  if config.is_error is not None:
    result["isError"] = config.is_error
  if config.meta is not None:
    result["_meta"] = config.meta
  return result


def build_output_schema_result(structured_content: object, extra_content: list | None = None) -> dict:
  """Build a successful result for a tool with an ``outputSchema``: ``structuredContent`` +
  a prepended JSON-text fallback. (§16.5, R-16.5-o/-p)
  """
  return build_call_tool_result(
    CallToolResultConfig(
      content=[structured_content_text_fallback(structured_content), *(extra_content or [])],
      structured_content=structured_content,
    )
  )


def build_tool_execution_error(message: str, *, content: list | None = None, structured_content: object = _OMITTED, meta: dict | None = None) -> dict:
  """Build a tool-execution-error result — a successful ``CallToolResult`` with
  ``isError: true`` (NOT a JSON-RPC error), so the model can observe and self-correct.
  (§16.6, R-16.6-b)
  """
  config = CallToolResultConfig(
    content=[{"type": "text", "text": message}, *(content or [])],
    is_error=True,
    meta=meta,
  )
  if structured_content is not _OMITTED:
    config = CallToolResultConfig(content=config.content, is_error=True, structured_content=structured_content, meta=meta)
  return build_call_tool_result(config)


# ─── §16.6 The two-layer error model ──────────────────────────────────────────

def build_unknown_tool_error(name: str) -> dict:
  """Build the JSON-RPC ``-32602`` error for an UNKNOWN tool name (never a CallToolResult).
  (§16.6, R-16.5-b, R-16.6-d/-e)
  """
  return {"code": INVALID_PARAMS_CODE, "message": f"Unknown tool: {name}"}


def build_invalid_arguments_error(name: str, errors: list[str] | None = None) -> dict:
  """Build the JSON-RPC ``-32602`` error for arguments that fail the tool's ``inputSchema``;
  the tool MUST NOT be invoked. (§16.6, R-16.5-d, R-16.6-d/-f)
  """
  detail = f": {'; '.join(errors)}" if errors else ""
  return {"code": INVALID_PARAMS_CODE, "message": f"Invalid arguments for tool {name}{detail}"}


@dataclass(frozen=True)
class ToolDispatch:
  """Outcome of :func:`dispatch_tool_call`.

  ``dispatched`` with ``tool`` + ``arguments`` on success; otherwise ``error`` carries the
  JSON-RPC protocol error to return. The two layers §16.6 keeps strictly distinct.
  """

  dispatched: bool
  tool: dict | None = None
  arguments: dict | None = None
  error: dict | None = None


def dispatch_tool_call(params: dict, exposed_tools: list[dict]) -> ToolDispatch:
  """Perform the §16.6 dispatch decision (no raising): unknown tool → ``-32602``; arguments
  failing the tool's ``inputSchema`` → ``-32602`` (tool NOT invoked); otherwise dispatched
  with resolved arguments. Tool names match case-sensitively. (§16.6, R-16.6-a/-d)
  """
  tool = next((t for t in exposed_tools if t.get("name") == params.get("name")), None)
  if tool is None:
    return ToolDispatch(False, error=build_unknown_tool_error(params.get("name")))
  args = resolve_call_tool_arguments(params)
  validation = validate_tool_arguments(tool, args)
  if not validation.valid:
    return ToolDispatch(False, error=build_invalid_arguments_error(tool["name"], validation.errors))
  return ToolDispatch(True, tool=tool, arguments=args)


# ─── §16.7 Tool annotations (untrusted hints) ─────────────────────────────────

#: The §16.7 default values for the four boolean annotation hints. (R-16.7-b–R-16.7-e)
TOOL_ANNOTATION_DEFAULTS = {
  "readOnlyHint": False,
  "destructiveHint": True,
  "idempotentHint": False,
  "openWorldHint": True,
}


def resolve_tool_annotation_hints(annotations: dict | None) -> dict:
  """Resolve the four boolean ``ToolAnnotations`` hints, applying the §16.7 defaults for
  absent fields. ``destructiveHint``/``idempotentHint`` are meaningful only when
  ``readOnlyHint`` is ``False``. (R-16.7-b–R-16.7-e)
  """
  annotations = annotations or {}
  return {
    "readOnlyHint": annotations.get("readOnlyHint", TOOL_ANNOTATION_DEFAULTS["readOnlyHint"]),
    "destructiveHint": annotations.get("destructiveHint", TOOL_ANNOTATION_DEFAULTS["destructiveHint"]),
    "idempotentHint": annotations.get("idempotentHint", TOOL_ANNOTATION_DEFAULTS["idempotentHint"]),
    "openWorldHint": annotations.get("openWorldHint", TOOL_ANNOTATION_DEFAULTS["openWorldHint"]),
  }


def may_trust_tool_annotations(server_is_trusted: bool = False) -> bool:
  """Return ``True`` ONLY when the server is explicitly trusted — annotations are untrusted
  hints, so safety decisions fail closed for any untrusted server. (§16.7, R-16.7-f/-g)
  """
  return server_is_trusted is True


# ─── §16.8 notifications/tools/list_changed ───────────────────────────────────

def is_tool_list_changed_notification(value: object) -> bool:
  """Return ``True`` for a well-formed list-changed notification (no ``id``). (§16.8)"""
  if not isinstance(value, dict) or value.get("jsonrpc") != "2.0":
    return False
  if value.get("method") != TOOLS_LIST_CHANGED_METHOD or "id" in value:
    return False
  return "params" not in value or isinstance(value["params"], dict)


def build_tool_list_changed_notification(meta: dict | None = None) -> dict:
  """Build a ``notifications/tools/list_changed`` notification; ``params`` only when ``_meta``
  is supplied. (§16.8, R-16.8-a/-b)
  """
  notification: dict = {"jsonrpc": "2.0", "method": TOOLS_LIST_CHANGED_METHOD}
  if meta is not None:
    notification["params"] = {"_meta": meta}
  return notification


def react_to_tool_list_changed() -> dict:
  """The prescribed client reaction: invalidate the cached tool list (SHOULD) and MAY
  re-list. (§16.8, R-16.8-c/-d)
  """
  return {"invalidateCachedToolList": True, "mayRelist": True}
