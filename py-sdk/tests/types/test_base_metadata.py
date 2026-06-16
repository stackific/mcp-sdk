"""Tests for BaseMetadata + display-name precedence (§14.1)."""

from mcp.types.base_metadata import is_valid_base_metadata, resolve_display_name


class TestIsValid:
  def test_minimal(self):
    assert is_valid_base_metadata({"name": "x"})

  def test_with_title(self):
    assert is_valid_base_metadata({"name": "x", "title": "X"})

  def test_invalid(self):
    assert not is_valid_base_metadata({})
    assert not is_valid_base_metadata({"name": 1})
    assert not is_valid_base_metadata({"name": "x", "title": 2})
    assert not is_valid_base_metadata("nope")


class TestResolveDisplayName:
  def test_title_wins(self):
    assert resolve_display_name("n", "T", "AT") == "T"

  def test_annotations_title_second(self):
    assert resolve_display_name("n", None, "AT") == "AT"

  def test_name_fallback(self):
    assert resolve_display_name("n") == "n"

  def test_empty_title_falls_through(self):
    assert resolve_display_name("n", "", "AT") == "AT"
    assert resolve_display_name("n", "", "") == "n"
