"""Tests for the _meta object + per-request envelope validation (§4.1–§4.3, §5.1)."""

import pytest

from mcp.protocol.errors import INVALID_PARAMS_CODE, MISSING_CLIENT_CAPABILITY_CODE
from mcp.protocol.meta import (
  CLIENT_CAPABILITIES_META_KEY,
  CLIENT_INFO_META_KEY,
  CURRENT_PROTOCOL_VERSION,
  PROTOCOL_VERSION_META_KEY,
  build_missing_capability_error,
  is_at_or_above_log_level,
  is_reserved_bare_key,
  is_supported_protocol_version,
  is_valid_revision_format,
  logging_level_index,
  validate_request_meta,
)


def valid_meta() -> dict:
  return {
    PROTOCOL_VERSION_META_KEY: "2026-07-28",
    CLIENT_INFO_META_KEY: {"name": "c", "version": "1.0"},
    CLIENT_CAPABILITIES_META_KEY: {},
  }


class TestReservedBareKeys:
  def test_membership(self):
    assert is_reserved_bare_key("progressToken")
    assert is_reserved_bare_key("traceparent")
    assert not is_reserved_bare_key("randomKey")


class TestLoggingLevels:
  def test_index_and_ordering(self):
    assert logging_level_index("debug") == 0
    assert logging_level_index("emergency") == 7
    assert is_at_or_above_log_level("error", "warning")
    assert not is_at_or_above_log_level("info", "error")

  def test_unknown_level_raises(self):
    with pytest.raises(ValueError):
      logging_level_index("verbose")


class TestProtocolVersion:
  def test_supported(self):
    assert is_supported_protocol_version(CURRENT_PROTOCOL_VERSION)
    assert not is_supported_protocol_version("2025-01-01")

  def test_revision_format(self):
    assert is_valid_revision_format("2026-07-28")
    assert not is_valid_revision_format("2026-7-28")
    assert not is_valid_revision_format("draft")


class TestValidateRequestMeta:
  def test_valid(self):
    assert validate_request_meta(valid_meta()).ok

  def test_missing_protocol_version(self):
    meta = valid_meta()
    del meta[PROTOCOL_VERSION_META_KEY]
    result = validate_request_meta(meta)
    assert not result.ok and result.code == INVALID_PARAMS_CODE

  def test_malformed_protocol_version(self):
    meta = valid_meta()
    meta[PROTOCOL_VERSION_META_KEY] = "not-a-date"
    result = validate_request_meta(meta)
    assert not result.ok and result.code == INVALID_PARAMS_CODE
    assert "revision identifier" in result.message

  def test_invalid_client_info(self):
    meta = valid_meta()
    meta[CLIENT_INFO_META_KEY] = {"name": "c"}  # missing version
    result = validate_request_meta(meta)
    assert not result.ok and result.code == INVALID_PARAMS_CODE

  def test_missing_capabilities(self):
    meta = valid_meta()
    meta[CLIENT_CAPABILITIES_META_KEY] = "nope"
    result = validate_request_meta(meta)
    assert not result.ok and result.code == INVALID_PARAMS_CODE

  def test_extra_keys_ignored(self):
    meta = valid_meta()
    meta["com.example/custom"] = {"x": 1}
    assert validate_request_meta(meta).ok


class TestMissingCapabilityError:
  def test_shape(self):
    err = build_missing_capability_error({"sampling": {}})
    assert err["code"] == MISSING_CLIENT_CAPABILITY_CODE
    assert err["data"]["requiredCapabilities"] == {"sampling": {}}
