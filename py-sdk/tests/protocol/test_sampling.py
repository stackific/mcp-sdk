"""Tests for Sampling (Deprecated) (§21.2)."""

from mcp.protocol.errors import INVALID_PARAMS_CODE
from mcp.protocol.sampling import (
  DEFAULT_TOOL_CHOICE,
  REQUIRED_CONSENT_OBLIGATIONS,
  SamplingConsentObligations,
  as_content_array,
  clamp_to_max_tokens,
  gate_sampling_tool_use,
  is_client_modifiable_request_field,
  is_deprecated_include_context,
  is_sampling_deprecated,
  is_standard_stop_reason,
  is_tool_enabled_request,
  is_tool_result_content,
  is_tool_use_content,
  is_valid_create_message_request_params,
  is_valid_sampling_content_block,
  is_valid_sampling_create_message_result,
  is_valid_sampling_message,
  may_server_send_sampling_request,
  preserve_content_meta,
  resolve_include_context,
  resolve_tool_choice,
  select_first_hint_match,
  unmet_required_consent_obligations,
  validate_sampling_message_ordering,
  validate_sampling_request,
  validate_tool_result_references,
  validate_user_tool_result_exclusivity,
  within_tool_loop_limit,
)

TOOL_USE = {"type": "tool_use", "id": "u1", "name": "calc", "input": {}}
TOOL_RESULT = {"type": "tool_result", "toolUseId": "u1", "content": []}
TEXT = {"type": "text", "text": "hi"}


class TestDeprecation:
  def test_deprecated(self):
    assert is_sampling_deprecated()


class TestContent:
  def test_tool_use(self):
    assert is_tool_use_content(TOOL_USE)
    assert not is_tool_use_content({"type": "tool_use", "id": "x"})

  def test_tool_result(self):
    assert is_tool_result_content(TOOL_RESULT)
    assert not is_tool_result_content({"type": "tool_result"})

  def test_content_block_union(self):
    assert is_valid_sampling_content_block(TEXT)
    assert is_valid_sampling_content_block(TOOL_USE)
    assert not is_valid_sampling_content_block({"type": "resource_link", "uri": "x", "name": "n"})

  def test_as_content_array(self):
    assert as_content_array(TEXT) == [TEXT]
    assert as_content_array([TEXT]) == [TEXT]

  def test_message(self):
    assert is_valid_sampling_message({"role": "user", "content": TEXT})
    assert is_valid_sampling_message({"role": "assistant", "content": [TOOL_USE]})
    assert not is_valid_sampling_message({"role": "system", "content": TEXT})


class TestModelPreferences:
  def test_hint_match_first(self):
    hints = [{"name": "haiku"}, {"name": "opus"}]
    assert select_first_hint_match(hints, ["claude-opus", "claude-haiku"]) == {"hint": {"name": "haiku"}, "model": "claude-haiku"}

  def test_no_match(self):
    assert select_first_hint_match([{"name": "z"}], ["a", "b"]) is None
    assert select_first_hint_match(None, ["a"]) is None


class TestToolChoiceAndContext:
  def test_resolve_tool_choice_default(self):
    assert resolve_tool_choice(None) == DEFAULT_TOOL_CHOICE
    assert resolve_tool_choice({"mode": "required"}) == {"mode": "required"}
    assert resolve_tool_choice({}) == {"mode": "auto"}

  def test_include_context(self):
    assert resolve_include_context({}) == "none"
    assert is_deprecated_include_context("allServers")
    assert not is_deprecated_include_context("none")


class TestRequestParams:
  def test_valid(self):
    assert is_valid_create_message_request_params({"messages": [{"role": "user", "content": TEXT}], "maxTokens": 100})

  def test_missing_required(self):
    assert not is_valid_create_message_request_params({"messages": []})  # no maxTokens
    assert not is_valid_create_message_request_params({"maxTokens": 1})  # no messages

  def test_tool_enabled(self):
    assert is_tool_enabled_request({"tools": []})
    assert is_tool_enabled_request({"toolChoice": {}})
    assert not is_tool_enabled_request({})

  def test_clamp(self):
    assert clamp_to_max_tokens(50, 100) == 50
    assert clamp_to_max_tokens(150, 100) == 100


class TestResult:
  def test_valid(self):
    assert is_valid_sampling_create_message_result({"role": "assistant", "content": TEXT, "model": "m", "resultType": "complete"})

  def test_standard_stop_reasons(self):
    assert is_standard_stop_reason("endTurn")
    assert not is_standard_stop_reason("custom")  # open string, but not "standard"

  def test_invalid(self):
    assert not is_valid_sampling_create_message_result({"role": "assistant", "content": TEXT, "model": "m"})  # no resultType


class TestGating:
  CAPS_TOOLS = {"sampling": {"tools": {}}}

  def test_gate_tool_use(self):
    assert gate_sampling_tool_use({"sampling": {}}, {}).ok  # not tool-enabled
    assert gate_sampling_tool_use(self.CAPS_TOOLS, {"tools": []}).ok
    res = gate_sampling_tool_use({"sampling": {}}, {"tools": []})
    assert not res.ok and res.error["code"] == INVALID_PARAMS_CODE

  def test_server_may_send(self):
    assert may_server_send_sampling_request({"sampling": {}}, {})
    assert not may_server_send_sampling_request({}, {})  # sampling not declared
    assert not may_server_send_sampling_request({"sampling": {}}, {"tools": []})  # tool-enabled, no sampling.tools
    assert not may_server_send_sampling_request({"sampling": {}}, {"includeContext": "allServers"})  # context gate

  def test_validate_request(self):
    ok = validate_sampling_request({"sampling": {}}, {"messages": [{"role": "user", "content": TEXT}], "maxTokens": 10})
    assert ok.ok
    bad = validate_sampling_request({"sampling": {}}, {"messages": []})
    assert not bad.ok


class TestContentConstraints:
  def test_user_exclusivity(self):
    assert validate_user_tool_result_exclusivity({"role": "user", "content": [TOOL_RESULT]})["ok"]
    assert not validate_user_tool_result_exclusivity({"role": "user", "content": [TOOL_RESULT, TEXT]})["ok"]
    assert validate_user_tool_result_exclusivity({"role": "user", "content": [TEXT]})["ok"]

  def test_ordering_valid(self):
    messages = [
      {"role": "assistant", "content": [TOOL_USE]},
      {"role": "user", "content": [TOOL_RESULT]},
    ]
    assert validate_sampling_message_ordering(messages)["ok"]

  def test_ordering_missing_followup(self):
    res = validate_sampling_message_ordering([{"role": "assistant", "content": [TOOL_USE]}])
    assert not res["ok"] and res["index"] == 0

  def test_ordering_unmatched_id(self):
    messages = [
      {"role": "assistant", "content": [TOOL_USE]},
      {"role": "user", "content": [{"type": "tool_result", "toolUseId": "other", "content": []}]},
    ]
    assert not validate_sampling_message_ordering(messages)["ok"]

  def test_tool_result_references(self):
    good = [{"role": "assistant", "content": [TOOL_USE]}, {"role": "user", "content": [TOOL_RESULT]}]
    assert validate_tool_result_references(good)["ok"]
    bad = [{"role": "user", "content": [TOOL_RESULT]}]  # result before any use
    assert not validate_tool_result_references(bad)["ok"]


class TestMetaAndConsent:
  def test_preserve_meta(self):
    block = {"type": "tool_use", "id": "u1", "name": "c", "input": {}, "_meta": {"k": 1}}
    copy = preserve_content_meta(block)
    assert copy == block and copy is not block
    assert preserve_content_meta(TEXT) is TEXT

  def test_client_modifiable_fields(self):
    assert is_client_modifiable_request_field("temperature")
    assert not is_client_modifiable_request_field("messages")

  def test_consent_obligations(self):
    assert "human_in_the_loop" in REQUIRED_CONSENT_OBLIGATIONS
    unmet = unmet_required_consent_obligations(SamplingConsentObligations())
    assert set(unmet) == set(REQUIRED_CONSENT_OBLIGATIONS)
    met = SamplingConsentObligations(human_in_the_loop=True, user_may_deny=True, handle_sensitive_data=True)
    assert unmet_required_consent_obligations(met) == []

  def test_tool_loop_limit(self):
    assert within_tool_loop_limit(2, 5)
    assert not within_tool_loop_limit(5, 5)
