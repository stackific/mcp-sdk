"""Tests for ResourceContents text/blob variants (§14.5)."""

from mcp.types.resource_contents import (
  is_valid_base64,
  is_valid_blob_resource_contents,
  is_valid_resource_contents,
  is_valid_text_resource_contents,
)


class TestBase64:
  def test_valid(self):
    assert is_valid_base64("aGVsbG8=")
    assert is_valid_base64("")  # empty is permitted by the pattern
    assert is_valid_base64("a-_b")  # url-safe variant

  def test_invalid(self):
    assert not is_valid_base64("not base64!")
    assert not is_valid_base64("***")


class TestText:
  def test_valid(self):
    assert is_valid_text_resource_contents({"uri": "file:///a", "text": "hi"})
    assert is_valid_text_resource_contents({"uri": "file:///a", "text": "hi", "mimeType": "text/plain", "_meta": {}})

  def test_invalid(self):
    assert not is_valid_text_resource_contents({"uri": "file:///a"})  # no text
    assert not is_valid_text_resource_contents({"text": "hi"})  # no uri
    assert not is_valid_text_resource_contents({"uri": 1, "text": "hi"})


class TestBlob:
  def test_valid(self):
    assert is_valid_blob_resource_contents({"uri": "file:///a", "blob": "aGVsbG8="})

  def test_invalid_base64(self):
    assert not is_valid_blob_resource_contents({"uri": "file:///a", "blob": "not!base64"})

  def test_missing_blob(self):
    assert not is_valid_blob_resource_contents({"uri": "file:///a"})


class TestUnion:
  def test_text_variant(self):
    assert is_valid_resource_contents({"uri": "u", "text": "t"})

  def test_blob_variant(self):
    assert is_valid_resource_contents({"uri": "u", "blob": "aGk="})

  def test_both_rejected(self):
    assert not is_valid_resource_contents({"uri": "u", "text": "t", "blob": "aGk="})

  def test_neither_rejected(self):
    assert not is_valid_resource_contents({"uri": "u"})
