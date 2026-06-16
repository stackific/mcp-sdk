"""Tests for Elicitation II — form schema, results, consent, security (§20.4–§20.8)."""

import pytest

from mcp.protocol.elicitation_form import (
  build_accept_result,
  build_cancel_result,
  build_decline_result,
  build_elicitation_complete_notification,
  build_url_accept_result,
  build_url_consent_presentation,
  check_elicitation_url_safety,
  classify_enum_schema,
  classify_primitive_schema,
  extract_defaults,
  find_sensitive_form_fields,
  handle_elicitation_complete,
  is_elicitation_complete_notification,
  is_legacy_titled_enum_schema,
  is_primitive_schema_definition,
  is_restricted_form_schema,
  may_render_url_clickable,
  resolve_elicit_action_outcome,
  validate_elicit_content,
  validate_elicit_result,
  validate_restricted_form_schema,
  verify_elicitation_user_binding,
)

STR = {"type": "string", "minLength": 2}
NUM = {"type": "integer", "minimum": 0, "maximum": 120}
BOOL = {"type": "boolean"}
ENUM = {"type": "string", "enum": ["a", "b"]}
TITLED = {"type": "string", "oneOf": [{"const": "a", "title": "A"}, {"const": "b", "title": "B"}]}
MULTI = {"type": "array", "items": {"type": "string", "enum": ["x", "y"]}, "minItems": 1}
LEGACY = {"type": "string", "enum": ["a"], "enumNames": ["A"]}
SCHEMA = {"type": "object", "properties": {"name": STR, "age": NUM}, "required": ["name"]}


class TestPrimitiveSchemas:
  def test_classify(self):
    assert classify_primitive_schema(STR) == "string"
    assert classify_primitive_schema(NUM) == "number"
    assert classify_primitive_schema(BOOL) == "boolean"
    assert classify_primitive_schema(ENUM) == "enum"
    assert classify_primitive_schema(TITLED) == "enum"
    assert classify_primitive_schema(MULTI) == "enum"
    assert classify_primitive_schema({"type": "object"}) is None

  def test_enum_forms(self):
    assert classify_enum_schema(ENUM) == "untitled-single-select"
    assert classify_enum_schema(TITLED) == "titled-single-select"
    assert classify_enum_schema(MULTI) == "untitled-multi-select"
    assert classify_enum_schema({"type": "array", "items": {"anyOf": [{"const": "a", "title": "A"}]}}) == "titled-multi-select"
    assert classify_enum_schema(LEGACY) == "legacy-titled"
    assert is_legacy_titled_enum_schema(LEGACY)

  def test_is_primitive(self):
    assert is_primitive_schema_definition(STR)
    assert is_primitive_schema_definition(ENUM)
    assert not is_primitive_schema_definition({"type": "object"})


class TestRestrictedSchema:
  def test_valid(self):
    assert validate_restricted_form_schema(SCHEMA).valid
    assert is_restricted_form_schema(SCHEMA)

  def test_rejects_nested_property(self):
    v = validate_restricted_form_schema({"type": "object", "properties": {"addr": {"type": "object"}}})
    assert not v.valid

  def test_rejects_undeclared_required(self):
    v = validate_restricted_form_schema({"type": "object", "properties": {"a": STR}, "required": ["b"]})
    assert not v.valid

  def test_extract_defaults(self):
    schema = {"type": "object", "properties": {"a": {"type": "string", "default": "x"}, "b": {"type": "boolean"}}}
    assert extract_defaults(schema) == {"a": "x"}


class TestContentConformance:
  def test_valid(self):
    assert validate_elicit_content({"name": "Ada", "age": 30}, SCHEMA).valid

  def test_missing_required(self):
    v = validate_elicit_content({"age": 30}, SCHEMA)
    assert not v.valid

  def test_unknown_field(self):
    v = validate_elicit_content({"name": "Ada", "extra": 1}, SCHEMA)
    assert not v.valid

  def test_type_mismatch(self):
    assert not validate_elicit_content({"name": "Ada", "age": "old"}, SCHEMA).valid

  def test_string_length(self):
    assert not validate_elicit_content({"name": "A"}, SCHEMA).valid  # minLength 2

  def test_number_bounds(self):
    assert not validate_elicit_content({"name": "Ada", "age": 200}, SCHEMA).valid

  def test_enum_membership(self):
    schema = {"type": "object", "properties": {"c": ENUM}}
    assert validate_elicit_content({"c": "a"}, schema).valid
    assert not validate_elicit_content({"c": "z"}, schema).valid

  def test_multi_select(self):
    schema = {"type": "object", "properties": {"m": MULTI}}
    assert validate_elicit_content({"m": ["x"]}, schema).valid
    assert not validate_elicit_content({"m": []}, schema).valid  # minItems 1
    assert not validate_elicit_content({"m": ["z"]}, schema).valid  # not a permitted value


class TestResultActions:
  def test_validate_form_accept(self):
    res = {"action": "accept", "content": {"name": "Ada"}}
    assert validate_elicit_result(res, "form", SCHEMA).valid

  def test_content_only_on_accept(self):
    assert not validate_elicit_result({"action": "decline", "content": {"name": "x"}}, "form").valid

  def test_no_content_on_url_accept(self):
    assert not validate_elicit_result({"action": "accept", "content": {"name": "x"}}, "url").valid

  def test_builders(self):
    assert build_accept_result({"name": "Ada"}, SCHEMA) == {"action": "accept", "content": {"name": "Ada"}}
    assert build_url_accept_result() == {"action": "accept"}
    assert build_decline_result() == {"action": "decline"}
    assert build_cancel_result() == {"action": "cancel"}

  def test_build_accept_invalid_raises(self):
    with pytest.raises(TypeError):
      build_accept_result({"name": "A"}, SCHEMA)  # too short

  def test_outcome(self):
    assert resolve_elicit_action_outcome({"action": "accept", "content": {"name": "Ada"}}, "form", SCHEMA).handle == "process-form-data"
    assert resolve_elicit_action_outcome({"action": "accept"}, "url").handle == "await-url-completion"
    assert resolve_elicit_action_outcome({"action": "decline"}, "form").handle == "declined"
    assert resolve_elicit_action_outcome({"action": "cancel"}, "form").handle == "cancelled"
    assert resolve_elicit_action_outcome({"action": "bogus"}, "form").handle == "malformed"


class TestCompleteNotification:
  def test_build_and_validate(self):
    note = build_elicitation_complete_notification("e1")
    assert is_elicitation_complete_notification(note)
    assert note["params"]["elicitationId"] == "e1"

  def test_empty_id_raises(self):
    with pytest.raises(TypeError):
      build_elicitation_complete_notification("")

  def test_handle(self):
    note = build_elicitation_complete_notification("e1")
    assert handle_elicitation_complete(note, {"e1": "pending"}).action == "complete"
    assert handle_elicitation_complete(note, {"e1": "completed"}).reason == "already-completed"
    assert handle_elicitation_complete(note, {}).reason == "unknown-id"


class TestSecurity:
  def test_sensitive_fields(self):
    schema = {"type": "object", "properties": {"password": {"type": "string"}, "name": {"type": "string"}}}
    assert find_sensitive_form_fields(schema) == ["password"]

  def test_url_safety(self):
    assert check_elicitation_url_safety("https://example.com/auth").safe
    assert not check_elicitation_url_safety("http://example.com/auth").safe  # insecure
    assert check_elicitation_url_safety("http://example.com/auth", allow_insecure=True).safe
    bad = check_elicitation_url_safety("https://u:p@example.com/auth?token=abc")
    assert not bad.safe
    reasons = {r["reason"] for r in bad.reasons}
    assert "pre-authenticated" in reasons and "contains-sensitive-info" in reasons

  def test_url_safety_invalid(self):
    assert not check_elicitation_url_safety("relative").safe

  def test_consent_presentation(self):
    p = build_url_consent_presentation("https://auth.example.com/login")
    assert p.host == "auth.example.com" and p.domain == "example.com" and p.scheme == "https"
    assert p.warnings == []

  def test_consent_punycode_warning(self):
    p = build_url_consent_presentation("https://xn--80ak6aa92e.com/login")
    assert p.contains_punycode and p.warnings

  def test_clickable(self):
    assert may_render_url_clickable("url", "url")
    assert not may_render_url_clickable("url", "form")
    assert not may_render_url_clickable("other", "url")

  def test_user_binding(self):
    assert verify_elicitation_user_binding("sub-1", "sub-1").ok
    assert verify_elicitation_user_binding("sub-1", "sub-2").reason == "subject-mismatch"
    assert verify_elicitation_user_binding(None, "sub-1").reason == "unverified-identity"
