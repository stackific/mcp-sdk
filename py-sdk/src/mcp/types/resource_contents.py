"""``ResourceContents`` — the concrete contents of a resource (§14.5).

Two mutually exclusive variants: ``TextResourceContents`` (carries ``text``) and
``BlobResourceContents`` (carries Base64 ``blob``). A value MUST NOT carry both
(R-14.5-h); a receiver selects the variant by which payload field is present (R-14.5-g).
"""

from __future__ import annotations

import re

# Accept standard (+/) and URL-safe (-_) Base64, with optional padding. (R-14.5-f)
_BASE64_RE = re.compile(r"^[A-Za-z0-9+/\-_]*(={0,2})?$")


def is_valid_base64(s: str) -> bool:
  """Return ``True`` when ``s`` contains only valid Base64 characters (R-14.5-f)."""
  return bool(_BASE64_RE.match(s))


def _common_ok(value: dict) -> bool:
  if not isinstance(value.get("uri"), str):
    return False
  if "mimeType" in value and not isinstance(value["mimeType"], str):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


def is_valid_text_resource_contents(value: object) -> bool:
  """Return ``True`` for valid ``TextResourceContents`` (REQUIRED ``uri`` + ``text``)."""
  return isinstance(value, dict) and isinstance(value.get("text"), str) and _common_ok(value)


def is_valid_blob_resource_contents(value: object) -> bool:
  """Return ``True`` for valid ``BlobResourceContents`` (REQUIRED ``uri`` + Base64 ``blob``)."""
  if not isinstance(value, dict):
    return False
  blob = value.get("blob")
  return isinstance(blob, str) and is_valid_base64(blob) and _common_ok(value)


def is_valid_resource_contents(value: object) -> bool:
  """Return ``True`` for valid ``ResourceContents`` — exactly one of ``text``/``blob`` (§14.5).

  A value carrying BOTH is rejected. (R-14.5-g, R-14.5-h)
  """
  if not isinstance(value, dict):
    return False
  has_text, has_blob = "text" in value, "blob" in value
  if has_text and has_blob:
    return False
  if has_text:
    return is_valid_text_resource_contents(value)
  if has_blob:
    return is_valid_blob_resource_contents(value)
  return False
