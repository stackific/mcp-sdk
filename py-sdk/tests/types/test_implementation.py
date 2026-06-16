"""Tests for the Implementation descriptor (§14.3)."""

import pytest

from mcp.types.implementation import (
  Implementation,
  is_valid_implementation,
  parse_implementation,
)


class TestIsValid:
  def test_minimal(self):
    assert is_valid_implementation({"name": "c", "version": "1.0"})

  def test_missing_name_or_version(self):
    assert not is_valid_implementation({"name": "c"})
    assert not is_valid_implementation({"version": "1.0"})

  def test_wrong_types(self):
    assert not is_valid_implementation({"name": 1, "version": "1.0"})
    assert not is_valid_implementation("nope")


class TestParse:
  def test_minimal(self):
    impl = parse_implementation({"name": "c", "version": "1.0"})
    assert impl == Implementation(name="c", version="1.0")

  def test_full_and_extras_preserved(self):
    impl = parse_implementation(
      {
        "name": "s",
        "title": "Server",
        "version": "2.4.1",
        "description": "d",
        "websiteUrl": "https://example.com",
        "vendor": "acme",  # forward-compatible extra (§2.3.4)
      }
    )
    assert impl.title == "Server"
    assert impl.website_url == "https://example.com"
    assert impl.extra == {"vendor": "acme"}

  def test_invalid_raises(self):
    with pytest.raises(ValueError):
      parse_implementation({"name": "c"})
