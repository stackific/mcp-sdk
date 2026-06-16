"""Tests for Role (§14.7) and Annotations (§14.6)."""

from mcp.types.annotations import is_valid_annotations
from mcp.types.role import is_role


class TestRole:
  def test_valid(self):
    assert is_role("user")
    assert is_role("assistant")

  def test_invalid(self):
    assert not is_role("system")
    assert not is_role("")
    assert not is_role(1)


class TestAnnotations:
  def test_empty_is_valid(self):
    assert is_valid_annotations({})

  def test_full(self):
    assert is_valid_annotations({"audience": ["user", "assistant"], "priority": 0.5, "lastModified": "2025-01-12T15:00:58Z"})

  def test_priority_bounds(self):
    assert is_valid_annotations({"priority": 0})
    assert is_valid_annotations({"priority": 1})
    assert not is_valid_annotations({"priority": 1.5})
    assert not is_valid_annotations({"priority": -0.1})

  def test_priority_bool_rejected(self):
    assert not is_valid_annotations({"priority": True})

  def test_bad_audience(self):
    assert not is_valid_annotations({"audience": ["user", "root"]})
    assert not is_valid_annotations({"audience": "user"})

  def test_bad_last_modified(self):
    assert not is_valid_annotations({"lastModified": 123})

  def test_extra_keys_allowed(self):
    assert is_valid_annotations({"com.example/x": 1})

  def test_non_object(self):
    assert not is_valid_annotations("nope")
