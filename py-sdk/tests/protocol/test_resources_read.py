"""Tests for Resources II — reading, not-found, notifications, URI schemes (§17.5–§17.9)."""

import pytest

from mcp.protocol.resources_read import (
  INODE_DIRECTORY_MIME_TYPE,
  LEGACY_RESOURCE_NOT_FOUND_CODE,
  RESOURCE_NOT_FOUND_CODE,
  RESOURCE_READ_INTERNAL_ERROR_CODE,
  ReadCacheHints,
  build_read_resource_request_params,
  build_read_resource_result,
  build_read_resource_retry_params,
  build_resource_list_changed_notification,
  build_resource_read_internal_error,
  build_resource_not_found_error,
  build_resource_updated_notification,
  is_custom_uri_scheme,
  is_https_resource_uri,
  is_input_required_read_result,
  is_resource_not_found_code,
  is_resource_updated_notification,
  is_valid_read_resource_result,
  may_fetch_directly,
  may_notify_resources_list_changed,
  recommended_uri_scheme,
  should_use_https_scheme,
  uri_scheme,
)

TEXT = {"uri": "file:///r", "text": "hi"}


class TestErrors:
  def test_not_found_code_acceptance(self):
    assert is_resource_not_found_code(RESOURCE_NOT_FOUND_CODE)
    assert is_resource_not_found_code(LEGACY_RESOURCE_NOT_FOUND_CODE)
    assert not is_resource_not_found_code(-32603)

  def test_build_not_found(self):
    err = build_resource_not_found_error("file:///x")
    assert err["code"] == RESOURCE_NOT_FOUND_CODE and err["data"]["uri"] == "file:///x"

  def test_build_internal(self):
    assert build_resource_read_internal_error()["code"] == RESOURCE_READ_INTERNAL_ERROR_CODE


class TestRequest:
  def test_build_params(self):
    assert build_read_resource_request_params("file:///r") == {"uri": "file:///r"}

  def test_bad_uri_raises(self):
    with pytest.raises(TypeError):
      build_read_resource_request_params("relative")

  def test_retry_params(self):
    params = build_read_resource_retry_params("file:///r", {"in-1": {}}, {"in-1": {"x": 1}}, request_state="OPAQUE")
    assert params["inputResponses"] == {"in-1": {"x": 1}} and params["requestState"] == "OPAQUE"

  def test_retry_missing_response_raises(self):
    with pytest.raises(ValueError):
      build_read_resource_retry_params("file:///r", {"in-1": {}}, {})


class TestResult:
  def test_build_and_validate(self):
    result = build_read_resource_result([TEXT], ReadCacheHints(0, "private"))
    assert is_valid_read_resource_result(result)

  def test_empty_contents_raises(self):
    with pytest.raises(ValueError):
      build_read_resource_result([], ReadCacheHints(0, "private"))

  def test_negative_ttl_raises(self):
    with pytest.raises(ValueError):
      build_read_resource_result([TEXT], ReadCacheHints(-1, "private"))

  def test_invalid_result_empty_contents(self):
    assert not is_valid_read_resource_result({"resultType": "complete", "contents": [], "ttlMs": 0, "cacheScope": "private"})

  def test_input_required_variant(self):
    assert is_input_required_read_result({"resultType": "input_required"})
    assert not is_input_required_read_result({"resultType": "complete"})


class TestNotifications:
  def test_list_changed(self):
    note = build_resource_list_changed_notification()
    assert note == {"method": "notifications/resources/list_changed"}

  def test_list_changed_with_meta(self):
    note = build_resource_list_changed_notification(meta={"k": 1})
    assert note["params"]["_meta"] == {"k": 1}

  def test_updated(self):
    note = build_resource_updated_notification("file:///r", "sub-1")
    assert is_resource_updated_notification(note)
    assert note["params"]["uri"] == "file:///r"
    assert note["params"]["_meta"]["io.modelcontextprotocol/subscriptionId"] == "sub-1"

  def test_list_changed_filter_gate(self):
    assert may_notify_resources_list_changed({"resourcesListChanged": True})
    assert not may_notify_resources_list_changed({})


class TestUriSchemes:
  def test_scheme_extraction(self):
    assert uri_scheme("file:///x") == "file"
    assert uri_scheme("Custom-App.v2://x") == "custom-app.v2"
    assert uri_scheme("not a uri") is None

  def test_custom_scheme(self):
    assert is_custom_uri_scheme("myapp://x")
    assert not is_custom_uri_scheme("file:///x")
    assert not is_custom_uri_scheme("relative")

  def test_https_and_direct_fetch(self):
    assert is_https_resource_uri("https://x/y")
    assert not is_https_resource_uri("file:///x")
    assert may_fetch_directly("https://x/y")
    assert not may_fetch_directly("file:///x")

  def test_scheme_guidance(self):
    assert recommended_uri_scheme(True)["scheme"] == "https"
    assert recommended_uri_scheme(False)["scheme"] == "non-https"
    assert should_use_https_scheme(True)
    assert not should_use_https_scheme(False)

  def test_inode_directory_constant(self):
    assert INODE_DIRECTORY_MIME_TYPE == "inode/directory"
