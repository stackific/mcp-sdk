"""Tests for Resources I — capability, URIs, types, listing (§17.1–§17.4)."""

import pytest

from mcp.protocol.resources import (
  ListCacheHints,
  build_list_resources_result,
  build_list_resource_templates_result,
  build_resources_capability,
  client_may_issue_resource_request,
  is_resource_uri,
  is_uri_template,
  is_valid_list_resources_result,
  is_valid_resource,
  is_valid_resource_template,
  may_accept_resource_request,
  may_emit_resource_updated,
  may_emit_resources_list_changed,
  resource_template_has_no_size,
  uri_template_variables,
)


class TestCapabilityGating:
  def test_gated_methods(self):
    assert may_accept_resource_request("resources/read", {"resources": {}})
    assert may_accept_resource_request("resources/list", {"resources": {}})
    assert not may_accept_resource_request("resources/read", {})
    assert not may_accept_resource_request("tools/call", {"resources": {}})
    assert client_may_issue_resource_request("resources/list", {"resources": {}})

  def test_notification_gating(self):
    assert may_emit_resources_list_changed({"resources": {"listChanged": True}})
    assert not may_emit_resources_list_changed({"resources": {}})
    assert may_emit_resource_updated({"resources": {"subscribe": True}})
    assert not may_emit_resource_updated({"resources": {}})

  def test_build_capability(self):
    assert build_resources_capability() == {}
    assert build_resources_capability(list_changed=True, subscribe=True) == {"listChanged": True, "subscribe": True}


class TestUri:
  def test_valid_resource_uri(self):
    assert is_resource_uri("file:///path")
    assert is_resource_uri("https://example.com/x")
    assert is_resource_uri("urn:isbn:0451450523")
    assert is_resource_uri("custom-app.v2://x")

  def test_invalid_resource_uri(self):
    assert not is_resource_uri("/relative/path")
    assert not is_resource_uri("noscheme")
    assert not is_resource_uri("file:")
    assert not is_resource_uri(123)


class TestUriTemplate:
  def test_valid(self):
    assert is_uri_template("file:///{path}")
    assert is_uri_template("db://{table}/{id}")
    assert is_uri_template("x://{+var}")
    assert is_uri_template("x://{var:3}")
    assert is_uri_template("x://{list*}")

  def test_invalid(self):
    assert not is_uri_template("x://{}")
    assert not is_uri_template("x://{unclosed")
    assert not is_uri_template("x://closed}")
    assert not is_uri_template("x://{a{b}}")

  def test_variables_extraction(self):
    assert uri_template_variables("db://{table}/{id}") == ["table", "id"]
    assert uri_template_variables("x://{+path}/{path}") == ["path"]  # dedup
    assert uri_template_variables("x://{a:3}/{b*}") == ["a", "b"]
    assert uri_template_variables("no/vars") == []


class TestTypes:
  def test_valid_resource(self):
    assert is_valid_resource({"name": "r", "uri": "file:///r"})
    assert is_valid_resource({"name": "r", "uri": "file:///r", "size": 10, "mimeType": "text/plain", "annotations": {"priority": 1}})

  def test_invalid_resource(self):
    assert not is_valid_resource({"name": "r"})  # no uri
    assert not is_valid_resource({"uri": "file:///r"})  # no name
    assert not is_valid_resource({"name": "r", "uri": "file:///r", "size": "big"})

  def test_valid_template(self):
    assert is_valid_resource_template({"name": "t", "uriTemplate": "x://{id}"})
    assert not is_valid_resource_template({"name": "t", "uriTemplate": "x://{}"})

  def test_template_has_no_size(self):
    assert resource_template_has_no_size({"name": "t", "uriTemplate": "x://{id}"})
    assert not resource_template_has_no_size({"name": "t", "uriTemplate": "x://{id}", "size": 1})


class TestListResults:
  def test_build_and_validate_resources(self):
    result = build_list_resources_result([{"name": "r", "uri": "file:///r"}], ListCacheHints(0, "private"))
    assert is_valid_list_resources_result(result) and result["resultType"] == "complete"

  def test_optional_fields(self):
    result = build_list_resources_result([], ListCacheHints(5, "public"), next_cursor="2", meta={"k": 1})
    assert result["nextCursor"] == "2" and result["_meta"] == {"k": 1}

  def test_negative_ttl_raises(self):
    with pytest.raises(ValueError):
      build_list_resources_result([], ListCacheHints(-1, "public"))

  def test_templates_result(self):
    result = build_list_resource_templates_result([{"name": "t", "uriTemplate": "x://{id}"}], ListCacheHints(0, "private"))
    assert result["resourceTemplates"][0]["name"] == "t"
