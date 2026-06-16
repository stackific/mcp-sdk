"""Tests for Elicitation I — capability, modes, request, gating, builders (§20.1–§20.3)."""

import pytest

from mcp.protocol.elicitation import (
  ELICITATION_CREATE_METHOD,
  build_form_elicit_request,
  build_url_elicit_request,
  client_supports_elicitation,
  client_supports_elicitation_mode,
  gate_elicitation_request,
  is_elicitation_create_request,
  is_valid_elicit_request,
  is_valid_elicitation_url,
  is_valid_requested_schema,
  may_server_send_elicitation,
  resolve_elicitation_mode,
  supported_elicitation_modes,
  validate_requested_schema,
)

SCHEMA = {"type": "object", "properties": {"name": {"type": "string"}}, "required": ["name"]}


class TestModes:
  def test_resolve_mode(self):
    assert resolve_elicitation_mode({}) == "form"  # absent ⇒ form
    assert resolve_elicitation_mode({"mode": "form"}) == "form"
    assert resolve_elicitation_mode({"mode": "url"}) == "url"
    assert resolve_elicitation_mode({"mode": "bogus"}) is None


class TestRequestedSchema:
  def test_valid(self):
    assert is_valid_requested_schema(SCHEMA)
    assert validate_requested_schema(SCHEMA).valid

  def test_rejects_non_object_root(self):
    assert not is_valid_requested_schema({"type": "string"})

  def test_flatness_rejects_nested(self):
    nested = {"type": "object", "properties": {"addr": {"type": "object"}}}
    v = validate_requested_schema(nested)
    assert not v.valid and any("primitive" in e["detail"] for e in v.errors)

  def test_flatness_rejects_nesting_keyword(self):
    v = validate_requested_schema({"type": "object", "properties": {"x": {"properties": {}}}})
    assert not v.valid

  def test_required_must_be_declared(self):
    v = validate_requested_schema({"type": "object", "properties": {"a": {"type": "string"}}, "required": ["b"]})
    assert not v.valid


class TestUrl:
  def test_valid(self):
    assert is_valid_elicitation_url("https://example.com/auth")
    assert is_valid_elicitation_url("mailto:a@b.com")

  def test_invalid(self):
    assert not is_valid_elicitation_url("/relative")
    assert not is_valid_elicitation_url("")


class TestRequest:
  def test_form_request(self):
    req = build_form_elicit_request(message="Name?", requested_schema=SCHEMA)
    assert is_valid_elicit_request(req)
    assert is_elicitation_create_request(req)
    assert "mode" not in req["params"]  # omitted by default

  def test_form_request_invalid_schema_raises(self):
    with pytest.raises(TypeError):
      build_form_elicit_request(message="x", requested_schema={"type": "object", "properties": {"a": {"type": "array"}}})

  def test_url_request(self):
    req = build_url_elicit_request(message="Authorize", elicitation_id="e1", url="https://example.com/auth")
    assert is_valid_elicit_request(req) and req["params"]["mode"] == "url"

  def test_url_request_bad_url_raises(self):
    with pytest.raises(TypeError):
      build_url_elicit_request(message="x", elicitation_id="e1", url="relative")

  def test_url_request_empty_id_raises(self):
    with pytest.raises(TypeError):
      build_url_elicit_request(message="x", elicitation_id="", url="https://x/y")

  def test_invalid_request(self):
    assert not is_valid_elicit_request({"method": "other", "params": {}})
    assert not is_valid_elicit_request({"method": ELICITATION_CREATE_METHOD, "params": {"message": 1}})


class TestCapability:
  def test_supports(self):
    assert client_supports_elicitation({"elicitation": {}})
    assert not client_supports_elicitation({})

  def test_supported_modes(self):
    assert supported_elicitation_modes({"elicitation": {}}) == ["form"]
    assert supported_elicitation_modes({"elicitation": {"url": {}}}) == ["form", "url"]
    assert supported_elicitation_modes({}) == []

  def test_supports_mode(self):
    assert client_supports_elicitation_mode({"elicitation": {}}, "form")
    assert not client_supports_elicitation_mode({"elicitation": {}}, "url")


class TestGating:
  def test_ok(self):
    assert gate_elicitation_request({"elicitation": {}}, "form").ok
    assert may_server_send_elicitation({"elicitation": {"url": {}}}, "url")

  def test_capability_not_declared(self):
    res = gate_elicitation_request({}, "form")
    assert not res.ok and res.rejection["reason"] == "capability-not-declared"

  def test_mode_not_supported(self):
    res = gate_elicitation_request({"elicitation": {}}, "url")
    assert not res.ok and res.rejection["reason"] == "mode-not-supported" and res.rejection["mode"] == "url"
