"""``Icon`` / ``Icons`` types with security validation (§14.2).

``Icon`` describes a single renderable icon image; ``Icons`` contributes an OPTIONAL
``icons`` array to identity/descriptor objects.

Security model (§14.2): only ``https:`` URLs and ``data:`` URIs are accepted (R-14.2-o);
unsafe schemes are rejected (R-14.2-n); MIME type is detected from magic bytes, not the
declared type (R-14.2-s); only allowlisted image types render (R-14.2-u).

The secure network fetch (``fetchIcon`` in the TS SDK) is deferred to a later cluster
with an injectable-fetch design; this module covers the synchronous validation core.
"""

from __future__ import annotations

import re

#: Background themes an icon may target (§14.2). Closed set.
ICON_THEMES = frozenset({"light", "dark"})

_SIZES_RE = re.compile(r"^\d+x\d+$|^any$")


def is_valid_icon(value: object) -> bool:
  """Return ``True`` for a valid ``Icon`` (§14.2): REQUIRED string ``src``; OPTIONAL
  ``mimeType`` (str), ``sizes`` (list of ``"WxH"``/``"any"``), ``theme`` (light/dark).
  Extra members tolerated.
  """
  if not isinstance(value, dict) or not isinstance(value.get("src"), str):
    return False
  if "mimeType" in value and not isinstance(value["mimeType"], str):
    return False
  if "sizes" in value:
    sizes = value["sizes"]
    if not isinstance(sizes, list) or not all(isinstance(s, str) and _SIZES_RE.match(s) for s in sizes):
      return False
  if "theme" in value and value["theme"] not in ICON_THEMES:
    return False
  return True


#: MIME types a consumer MUST support when rendering icons. (R-14.2-l)
REQUIRED_IMAGE_TYPES = frozenset({"image/png", "image/jpeg", "image/jpg"})
#: MIME types a consumer SHOULD additionally support. (R-14.2-m)
RECOMMENDED_IMAGE_TYPES = frozenset({"image/svg+xml", "image/webp"})
#: Default allowlist: REQUIRED + RECOMMENDED. (R-14.2-u)
DEFAULT_IMAGE_ALLOWLIST = REQUIRED_IMAGE_TYPES | RECOMMENDED_IMAGE_TYPES


class IconValidationError(Exception):
  """Raised when an icon URI or its content is rejected for security reasons."""

  def __init__(self, src: str, reason: str) -> None:
    super().__init__(f"Icon rejected ({reason}): {src}")
    self.src = src


def validate_icon_src(src: str) -> None:
  """Validate an icon ``src`` URI scheme (§14.2): only ``https:`` or ``data:`` accepted.

  :raises IconValidationError: when the scheme is missing or not permitted. (R-14.2-o,
    R-14.2-n)
  """
  colon = src.find(":")
  if colon == -1:
    raise IconValidationError(src, "no URI scheme present")
  scheme = src[: colon + 1].lower()
  if scheme not in ("https:", "data:"):
    raise IconValidationError(src, f"scheme '{scheme}' is not permitted; only https: and data: are accepted")


def is_valid_icon_src(src: str) -> bool:
  """Return ``True`` when ``src`` passes :func:`validate_icon_src` without raising."""
  try:
    validate_icon_src(src)
    return True
  except IconValidationError:
    return False


#: Magic-byte signatures for supported image types. (R-14.2-s)
MAGIC_BYTES: tuple[tuple[str, bytes], ...] = (
  ("image/png", b"\x89PNG\r\n\x1a\n"),
  ("image/jpeg", b"\xff\xd8\xff"),
  ("image/gif", b"GIF"),
  ("image/webp", b"RIFF"),  # RIFF container; bytes 8-11 must be 'WEBP'
)


def detect_mime_type_from_magic_bytes(data: bytes) -> str | None:
  """Detect an image's MIME type from its magic bytes, treating any declared type as
  advisory (R-14.2-s). Returns ``None`` when no known signature matches.
  """
  for mime_type, signature in MAGIC_BYTES:
    if data[: len(signature)] == signature:
      if mime_type == "image/webp" and data[8:12] != b"WEBP":
        continue
      return mime_type
  # SVG is XML-based (no magic bytes) — detect by leading text.
  if len(data) >= 4:
    head = data[:100].decode("utf-8", errors="ignore").lstrip().lower()
    if head.startswith("<?xml") or head.startswith("<svg"):
      return "image/svg+xml"
  return None


def validate_icon_bytes(
  data: bytes,
  declared_mime_type: str | None = None,
  allowed_types: frozenset[str] | set[str] = DEFAULT_IMAGE_ALLOWLIST,
) -> str:
  """Validate icon byte content before rendering (§14.2). Returns the detected MIME type.

  Detects the actual type from magic bytes (ignoring the declared type), rejects unknown
  or non-allowlisted types, and — when ``declared_mime_type`` is given — rejects a
  mismatch (``image/jpg`` is normalised to ``image/jpeg``). (R-14.2-r–R-14.2-u)

  :raises IconValidationError: on any validation failure.
  """
  detected = detect_mime_type_from_magic_bytes(data)
  if detected is None:
    raise IconValidationError("(bytes)", "unknown image type; cannot render")
  if detected not in allowed_types:
    raise IconValidationError("(bytes)", f"image type {detected} is not on the allowlist")
  if declared_mime_type:
    def _norm(t: str) -> str:
      return "image/jpeg" if t == "image/jpg" else t

    if _norm(detected) != _norm(declared_mime_type):
      raise IconValidationError(
        "(bytes)", f"MIME type mismatch: declared '{declared_mime_type}', detected '{detected}'"
      )
  return detected
