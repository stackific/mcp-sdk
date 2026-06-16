"""Tests for the ContentBlock union (§14.4, §14.8)."""

from mcp.types.content import (
  is_forbidden_content_block_type,
  is_known_content_block_type,
  is_valid_audio_content,
  is_valid_content_block,
  is_valid_embedded_resource,
  is_valid_image_content,
  is_valid_resource_link,
  is_valid_text_content,
)


class TestTypePredicates:
  def test_known(self):
    for t in ("text", "image", "audio", "resource_link", "resource"):
      assert is_known_content_block_type(t)
    assert not is_known_content_block_type("widget")

  def test_forbidden(self):
    assert is_forbidden_content_block_type("tool_use")
    assert is_forbidden_content_block_type("tool_result")
    assert not is_forbidden_content_block_type("text")


class TestText:
  def test_valid(self):
    assert is_valid_text_content({"type": "text", "text": "hi"})
    assert is_valid_text_content({"type": "text", "text": "hi", "annotations": {"priority": 1}, "_meta": {}})

  def test_invalid(self):
    assert not is_valid_text_content({"type": "text"})
    assert not is_valid_text_content({"type": "image", "text": "hi"})
    assert not is_valid_text_content({"type": "text", "text": "hi", "annotations": {"priority": 2}})


class TestMedia:
  def test_image(self):
    assert is_valid_image_content({"type": "image", "data": "aGk=", "mimeType": "image/png"})
    assert not is_valid_image_content({"type": "image", "data": "not!b64", "mimeType": "image/png"})
    assert not is_valid_image_content({"type": "image", "data": "aGk="})  # no mimeType

  def test_audio(self):
    assert is_valid_audio_content({"type": "audio", "data": "aGk=", "mimeType": "audio/wav"})


class TestResourceLink:
  def test_valid(self):
    assert is_valid_resource_link({"type": "resource_link", "uri": "file:///a", "name": "a"})
    assert is_valid_resource_link(
      {"type": "resource_link", "uri": "u", "name": "n", "title": "T", "mimeType": "text/plain", "size": 12, "icons": []}
    )

  def test_invalid(self):
    assert not is_valid_resource_link({"type": "resource_link", "uri": "u"})  # no name
    assert not is_valid_resource_link({"type": "resource_link", "uri": "u", "name": "n", "size": "big"})


class TestEmbeddedResource:
  def test_valid(self):
    assert is_valid_embedded_resource({"type": "resource", "resource": {"uri": "u", "text": "t"}})

  def test_invalid_contents(self):
    assert not is_valid_embedded_resource({"type": "resource", "resource": {"uri": "u", "text": "t", "blob": "aGk="}})


class TestUnion:
  def test_known_dispatch(self):
    assert is_valid_content_block({"type": "text", "text": "hi"})
    assert is_valid_content_block({"type": "resource", "resource": {"uri": "u", "blob": "aGk="}})

  def test_malformed_known_rejected(self):
    assert not is_valid_content_block({"type": "image", "data": "aGk="})  # missing mimeType

  def test_unknown_type_is_forward_compatible(self):
    assert is_valid_content_block({"type": "future_widget", "payload": 1})

  def test_forbidden_sampling_types_rejected(self):
    assert not is_valid_content_block({"type": "tool_use", "id": "x"})
    assert not is_valid_content_block({"type": "tool_result"})

  def test_non_object(self):
    assert not is_valid_content_block("nope")
    assert not is_valid_content_block({"text": "no type"})
