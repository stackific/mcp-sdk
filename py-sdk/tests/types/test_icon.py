"""Tests for Icon validation: schema, src security, and magic-byte detection (§14.2)."""

import pytest

from mcp.types.icon import (
  DEFAULT_IMAGE_ALLOWLIST,
  IconValidationError,
  detect_mime_type_from_magic_bytes,
  is_valid_icon,
  is_valid_icon_src,
  validate_icon_bytes,
  validate_icon_src,
)

PNG = b"\x89PNG\r\n\x1a\n" + b"\x00" * 8
JPEG = b"\xff\xd8\xff" + b"\x00" * 8
GIF = b"GIF89a" + b"\x00" * 8
WEBP = b"RIFF\x00\x00\x00\x00WEBP" + b"\x00" * 8
SVG = b"<svg xmlns='http://www.w3.org/2000/svg'></svg>"


class TestIconSchema:
  def test_minimal(self):
    assert is_valid_icon({"src": "https://example.com/i.png"})

  def test_full(self):
    assert is_valid_icon({"src": "data:image/png;base64,aGk=", "mimeType": "image/png", "sizes": ["48x48", "any"], "theme": "dark"})

  def test_invalid(self):
    assert not is_valid_icon({})
    assert not is_valid_icon({"src": 1})
    assert not is_valid_icon({"src": "x", "sizes": ["bad"]})
    assert not is_valid_icon({"src": "x", "theme": "blue"})


class TestSrcSecurity:
  def test_https_and_data_allowed(self):
    validate_icon_src("https://example.com/i.png")
    validate_icon_src("data:image/png;base64,aGk=")
    assert is_valid_icon_src("https://x/y") and is_valid_icon_src("data:,abc")

  def test_unsafe_schemes_rejected(self):
    for src in ("http://x/y", "javascript:alert(1)", "file:///etc/passwd", "ftp://x/y", "ws://x"):
      assert not is_valid_icon_src(src)
      with pytest.raises(IconValidationError):
        validate_icon_src(src)

  def test_no_scheme_rejected(self):
    assert not is_valid_icon_src("/relative/path.png")


class TestMagicBytes:
  def test_detects_known_types(self):
    assert detect_mime_type_from_magic_bytes(PNG) == "image/png"
    assert detect_mime_type_from_magic_bytes(JPEG) == "image/jpeg"
    assert detect_mime_type_from_magic_bytes(GIF) == "image/gif"
    assert detect_mime_type_from_magic_bytes(WEBP) == "image/webp"
    assert detect_mime_type_from_magic_bytes(SVG) == "image/svg+xml"

  def test_webp_requires_webp_tag(self):
    riff_not_webp = b"RIFF\x00\x00\x00\x00AVI " + b"\x00" * 8
    assert detect_mime_type_from_magic_bytes(riff_not_webp) is None

  def test_unknown(self):
    assert detect_mime_type_from_magic_bytes(b"\x00\x01\x02\x03") is None


class TestValidateBytes:
  def test_allowlisted_ok(self):
    assert validate_icon_bytes(PNG) == "image/png"

  def test_detected_not_on_allowlist(self):
    with pytest.raises(IconValidationError):
      validate_icon_bytes(GIF)  # gif detected but not in DEFAULT_IMAGE_ALLOWLIST

  def test_unknown_rejected(self):
    with pytest.raises(IconValidationError):
      validate_icon_bytes(b"\x00\x01\x02\x03")

  def test_declared_mismatch_rejected(self):
    with pytest.raises(IconValidationError):
      validate_icon_bytes(PNG, declared_mime_type="image/webp")

  def test_jpg_normalises_to_jpeg(self):
    assert validate_icon_bytes(JPEG, declared_mime_type="image/jpg") == "image/jpeg"

  def test_default_allowlist_membership(self):
    assert "image/png" in DEFAULT_IMAGE_ALLOWLIST and "image/gif" not in DEFAULT_IMAGE_ALLOWLIST
