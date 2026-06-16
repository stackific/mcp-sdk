"""Tests for Prompts — capability, types, list/get, errors, list-changed (§18)."""

import pytest

from mcp.protocol.prompts import (
  GetPromptResultConfig,
  ListPromptsResultConfig,
  PROMPTS_INTERNAL_ERROR_CODE,
  PROMPTS_INVALID_PARAMS_CODE,
  build_get_prompt_result,
  build_list_prompts_result,
  build_missing_argument_error,
  build_prompt_internal_error,
  build_prompt_list_changed_notification,
  build_unknown_prompt_error,
  discriminate_get_prompt_response,
  is_valid_get_prompt_result,
  is_valid_list_prompts_result,
  is_valid_prompt,
  is_valid_prompt_argument,
  is_valid_prompt_list_changed_notification,
  is_valid_prompt_message,
  may_call_prompt_method,
  may_complete_prompt_argument,
  may_expect_prompts_list_changed,
  required_argument_names,
  resolve_get_prompt_result_type,
  server_declares_prompts,
  validate_get_prompt_request,
)

PROMPT = {"name": "greet", "arguments": [{"name": "who", "required": True}, {"name": "style"}]}
MSG = {"role": "user", "content": {"type": "text", "text": "hi"}}


class TestCapability:
  def test_declares_and_gating(self):
    assert server_declares_prompts({"prompts": {}})
    assert may_call_prompt_method("prompts/get", {"prompts": {}})
    assert not may_call_prompt_method("prompts/get", {})
    assert may_expect_prompts_list_changed({"prompts": {"listChanged": True}})
    assert not may_expect_prompts_list_changed({"prompts": {}})


class TestTypes:
  def test_prompt_argument(self):
    assert is_valid_prompt_argument({"name": "x", "required": True, "description": "d"})
    assert not is_valid_prompt_argument({"required": True})  # no name
    assert not is_valid_prompt_argument({"name": "x", "required": "yes"})

  def test_prompt(self):
    assert is_valid_prompt(PROMPT)
    assert is_valid_prompt({"name": "p"})
    assert not is_valid_prompt({"name": "p", "arguments": [{"required": True}]})

  def test_required_argument_names(self):
    assert required_argument_names(PROMPT) == ["who"]
    assert required_argument_names({"name": "p"}) == []

  def test_prompt_message(self):
    assert is_valid_prompt_message(MSG)
    assert not is_valid_prompt_message({"role": "system", "content": {"type": "text", "text": "x"}})
    assert not is_valid_prompt_message({"role": "user", "content": [MSG]})  # must be a single block


class TestListResult:
  def test_build_and_validate(self):
    result = build_list_prompts_result(ListPromptsResultConfig(prompts=[PROMPT], ttl_ms=0, cache_scope="private"))
    assert is_valid_list_prompts_result(result)

  def test_negative_ttl_raises(self):
    with pytest.raises(ValueError):
      build_list_prompts_result(ListPromptsResultConfig(prompts=[], ttl_ms=-1, cache_scope="public"))


class TestGetResult:
  def test_build_and_validate(self):
    result = build_get_prompt_result(GetPromptResultConfig(messages=[MSG], description="d"))
    assert is_valid_get_prompt_result(result) and result["description"] == "d"

  def test_resolve_result_type_absent_is_complete(self):
    assert resolve_get_prompt_result_type({}) == "complete"
    assert resolve_get_prompt_result_type({"resultType": "input_required"}) == "input_required"

  def test_absent_result_type_validates(self):
    assert is_valid_get_prompt_result({"messages": [MSG]})


class TestDiscrimination:
  def test_complete(self):
    d = discriminate_get_prompt_response({"resultType": "complete", "messages": [MSG]})
    assert d.kind == "complete"

  def test_absent_treated_as_complete(self):
    d = discriminate_get_prompt_response({"messages": [MSG]})
    assert d.kind == "complete"

  def test_input_required(self):
    d = discriminate_get_prompt_response({"resultType": "input_required", "inputRequests": {}})
    assert d.kind == "input_required"

  def test_unrecognized_is_error(self):
    d = discriminate_get_prompt_response({"resultType": "frobnicate"})
    assert d.kind == "error" and d.result_type == "frobnicate"

  def test_malformed_complete_is_error(self):
    d = discriminate_get_prompt_response({"resultType": "complete", "messages": "no"})
    assert d.kind == "error"


class TestErrorModel:
  def test_unknown_prompt(self):
    assert build_unknown_prompt_error("ghost")["code"] == PROMPTS_INVALID_PARAMS_CODE

  def test_missing_argument(self):
    err = build_missing_argument_error(["who"])
    assert err["code"] == PROMPTS_INVALID_PARAMS_CODE and "who" in err["message"]

  def test_internal(self):
    assert build_prompt_internal_error("db down")["code"] == PROMPTS_INTERNAL_ERROR_CODE


class TestValidateRequest:
  def test_unknown_name(self):
    v = validate_get_prompt_request({"name": "ghost"}, [PROMPT])
    assert not v.ok and v.error["code"] == PROMPTS_INVALID_PARAMS_CODE

  def test_missing_required(self):
    v = validate_get_prompt_request({"name": "greet", "arguments": {}}, [PROMPT])
    assert not v.ok and "who" in v.error["message"]

  def test_valid(self):
    v = validate_get_prompt_request({"name": "greet", "arguments": {"who": "Ada"}}, [PROMPT])
    assert v.ok and v.arguments == {"who": "Ada"}

  def test_offered_as_map(self):
    v = validate_get_prompt_request({"name": "greet", "arguments": {"who": "Ada"}}, {"greet": PROMPT})
    assert v.ok


class TestListChanged:
  def test_build_and_validate(self):
    note = build_prompt_list_changed_notification()
    assert is_valid_prompt_list_changed_notification(note) and "id" not in note

  def test_with_meta(self):
    note = build_prompt_list_changed_notification({"k": 1})
    assert note["params"]["_meta"] == {"k": 1}

  def test_rejects_request_with_id(self):
    assert not is_valid_prompt_list_changed_notification({"jsonrpc": "2.0", "id": 1, "method": "notifications/prompts/list_changed"})

  def test_may_complete(self):
    assert may_complete_prompt_argument()
