"""Tests for Tools II — calling, the two-layer error model, annotations, list-changed (§16.5–§16.9)."""

import json

import pytest

from mcp.protocol.errors import INVALID_PARAMS_CODE
from mcp.protocol.tools_call import (
  CallToolRequestConfig,
  CallToolResultConfig,
  CallToolRetryConfig,
  TOOL_ANNOTATION_DEFAULTS,
  build_call_tool_request,
  build_call_tool_result,
  build_call_tool_retry_request,
  build_invalid_arguments_error,
  build_output_schema_result,
  build_tool_execution_error,
  build_tool_list_changed_notification,
  build_unknown_tool_error,
  dispatch_tool_call,
  is_call_tool_error,
  is_call_tool_request,
  is_call_tool_result,
  is_structured_content_present,
  is_tool_list_changed_notification,
  may_trust_tool_annotations,
  react_to_tool_list_changed,
  resolve_call_tool_arguments,
  resolve_tool_annotation_hints,
  structured_content_text_fallback,
)

TOOL = {"name": "add", "inputSchema": {"type": "object", "properties": {"a": {"type": "number"}}, "required": ["a"]}}


class TestRequest:
  def test_is_call_tool_request(self):
    assert is_call_tool_request({"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "x"}})
    assert not is_call_tool_request({"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {}})
    assert not is_call_tool_request({"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "x"}})

  def test_resolve_arguments_defaults_to_empty(self):
    assert resolve_call_tool_arguments({"name": "x"}) == {}
    assert resolve_call_tool_arguments({"name": "x", "arguments": {"a": 1}}) == {"a": 1}

  def test_build_request(self):
    req = build_call_tool_request(1, CallToolRequestConfig(name="add", arguments={"a": 1}))
    assert req == {"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "add", "arguments": {"a": 1}}}

  def test_build_request_omits_arguments(self):
    req = build_call_tool_request(1, CallToolRequestConfig(name="add"))
    assert "arguments" not in req["params"]

  def test_retry_request_needs_fresh_id(self):
    req = build_call_tool_retry_request(1, 2, CallToolRetryConfig(name="add", input_responses={"in-1": {}}, request_state="OPAQUE"))
    assert req["id"] == 2 and req["params"]["requestState"] == "OPAQUE"
    assert req["params"]["inputResponses"] == {"in-1": {}}

  def test_retry_same_id_raises(self):
    with pytest.raises(ValueError):
      build_call_tool_retry_request(1, 1, CallToolRetryConfig(name="add", input_responses={}))


class TestResult:
  def test_build_and_validate(self):
    result = build_call_tool_result(CallToolResultConfig(content=[{"type": "text", "text": "hi"}]))
    assert is_call_tool_result(result) and result["resultType"] == "complete"
    assert not is_call_tool_error(result)

  def test_explicit_null_structured_content_survives(self):
    result = build_call_tool_result(CallToolResultConfig(content=[], structured_content=None))
    assert is_structured_content_present(result) and result["structuredContent"] is None

  def test_omitted_structured_content_absent(self):
    result = build_call_tool_result(CallToolResultConfig(content=[]))
    assert not is_structured_content_present(result)

  def test_text_fallback(self):
    block = structured_content_text_fallback({"sum": 5})
    assert block["type"] == "text" and json.loads(block["text"]) == {"sum": 5}

  def test_output_schema_result(self):
    result = build_output_schema_result({"sum": 5})
    assert result["structuredContent"] == {"sum": 5}
    assert json.loads(result["content"][0]["text"]) == {"sum": 5}

  def test_execution_error(self):
    result = build_tool_execution_error("boom")
    assert is_call_tool_result(result) and is_call_tool_error(result)
    assert result["content"][0]["text"] == "boom"


class TestTwoLayerErrors:
  def test_unknown_tool_error(self):
    assert build_unknown_tool_error("ghost") == {"code": INVALID_PARAMS_CODE, "message": "Unknown tool: ghost"}

  def test_invalid_arguments_error(self):
    err = build_invalid_arguments_error("add", ["a is required"])
    assert err["code"] == INVALID_PARAMS_CODE and "a is required" in err["message"]


class TestDispatch:
  def test_dispatched(self):
    d = dispatch_tool_call({"name": "add", "arguments": {"a": 1}}, [TOOL])
    assert d.dispatched and d.tool is TOOL and d.arguments == {"a": 1}

  def test_unknown_tool(self):
    d = dispatch_tool_call({"name": "ghost"}, [TOOL])
    assert not d.dispatched and d.error["code"] == INVALID_PARAMS_CODE

  def test_invalid_arguments_not_dispatched(self):
    d = dispatch_tool_call({"name": "add", "arguments": {"a": "no"}}, [TOOL])
    assert not d.dispatched and d.error["code"] == INVALID_PARAMS_CODE

  def test_omitted_arguments_default_validated(self):
    d = dispatch_tool_call({"name": "add"}, [TOOL])  # missing required 'a'
    assert not d.dispatched


class TestAnnotations:
  def test_defaults_applied(self):
    hints = resolve_tool_annotation_hints(None)
    assert hints == TOOL_ANNOTATION_DEFAULTS

  def test_overrides(self):
    hints = resolve_tool_annotation_hints({"readOnlyHint": True})
    assert hints["readOnlyHint"] is True and hints["openWorldHint"] is True

  def test_untrusted_fails_closed(self):
    assert not may_trust_tool_annotations()
    assert not may_trust_tool_annotations(False)
    assert may_trust_tool_annotations(True)


class TestListChanged:
  def test_build_and_validate(self):
    note = build_tool_list_changed_notification()
    assert is_tool_list_changed_notification(note) and "id" not in note

  def test_with_meta(self):
    note = build_tool_list_changed_notification({"k": 1})
    assert note["params"]["_meta"] == {"k": 1}

  def test_rejects_request_with_id(self):
    assert not is_tool_list_changed_notification({"jsonrpc": "2.0", "id": 1, "method": "notifications/tools/list_changed"})

  def test_reaction(self):
    assert react_to_tool_list_changed() == {"invalidateCachedToolList": True, "mayRelist": True}
