"""``ContentBlock`` — the discriminated-union payload of tool-call results and prompt
messages (§14.4).

Five known member types, dispatched by the case-sensitive ``type`` field. An unknown
``type`` is treated as unsupported content (forward-compatible) rather than failing the
whole message (R-14.4-a, R-14.4-b) — EXCEPT the deprecated sampling types ``tool_use`` /
``tool_result``, which MUST NOT appear here (R-14.8-a, R-14.8-b).

``ResourceLink.icons`` is validated here as an optional list; per-``Icon`` validation
lives in :mod:`mcp.types.icon`.
"""

from __future__ import annotations

from mcp.types.annotations import is_valid_annotations
from mcp.types.resource_contents import is_valid_base64, is_valid_resource_contents

#: Known ContentBlock discriminators. (§14.4)
KNOWN_CONTENT_BLOCK_TYPES = ("text", "image", "audio", "resource_link", "resource")

#: ``type`` values from the deprecated sampling capability that MUST NOT appear. (R-14.8-a/-b)
FORBIDDEN_CONTENT_BLOCK_TYPES = frozenset({"tool_use", "tool_result"})


def is_known_content_block_type(type_: str) -> bool:
  """Return ``True`` when ``type_`` is a known, supported ``ContentBlock`` type. (R-14.4-b)"""
  return type_ in KNOWN_CONTENT_BLOCK_TYPES


def is_forbidden_content_block_type(type_: str) -> bool:
  """Return ``True`` when ``type_`` is a forbidden sampling content type. (R-14.8-a/-b)"""
  return type_ in FORBIDDEN_CONTENT_BLOCK_TYPES


def _is_number(value: object) -> bool:
  return isinstance(value, (int, float)) and not isinstance(value, bool)


def _tail_ok(value: dict) -> bool:
  """Validate the shared optional tail every block may carry (``annotations``, ``_meta``)."""
  if "annotations" in value and not is_valid_annotations(value["annotations"]):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


def is_valid_text_content(value: object) -> bool:
  """Inline text content (§14.4.1): ``type:"text"`` + REQUIRED string ``text``."""
  return (
    isinstance(value, dict)
    and value.get("type") == "text"
    and isinstance(value.get("text"), str)
    and _tail_ok(value)
  )


def _is_valid_media_content(value: object, type_: str) -> bool:
  return (
    isinstance(value, dict)
    and value.get("type") == type_
    and isinstance(value.get("data"), str)
    and is_valid_base64(value["data"])
    and isinstance(value.get("mimeType"), str)
    and _tail_ok(value)
  )


def is_valid_image_content(value: object) -> bool:
  """Inline image content (§14.4.2): ``type:"image"`` + Base64 ``data`` + ``mimeType``."""
  return _is_valid_media_content(value, "image")


def is_valid_audio_content(value: object) -> bool:
  """Inline audio content (§14.4.3): ``type:"audio"`` + Base64 ``data`` + ``mimeType``."""
  return _is_valid_media_content(value, "audio")


def is_valid_resource_link(value: object) -> bool:
  """A resource reference by URI (§14.4.4): ``type:"resource_link"`` + ``uri`` + ``name``.

  Optional ``title``/``description``/``mimeType`` (strings), ``icons`` (list), ``size``
  (number).
  """
  if not isinstance(value, dict) or value.get("type") != "resource_link":
    return False
  if not isinstance(value.get("uri"), str) or not isinstance(value.get("name"), str):
    return False
  for key in ("title", "description", "mimeType"):
    if key in value and not isinstance(value[key], str):
      return False
  if "icons" in value and not isinstance(value["icons"], list):
    return False
  if "size" in value and not _is_number(value["size"]):
    return False
  return _tail_ok(value)


def is_valid_embedded_resource(value: object) -> bool:
  """An embedded resource (§14.4.5): ``type:"resource"`` + a valid ``ResourceContents``."""
  return (
    isinstance(value, dict)
    and value.get("type") == "resource"
    and is_valid_resource_contents(value.get("resource"))
    and _tail_ok(value)
  )


def is_valid_content_block(value: object) -> bool:
  """Return ``True`` for a valid ``ContentBlock`` (§14.4).

  Known types are validated strictly; an unknown ``type`` is accepted as
  forward-compatible unsupported content (R-14.4-b) UNLESS it is a forbidden sampling
  type (R-14.8-a/-b).
  """
  if not isinstance(value, dict) or not isinstance(value.get("type"), str):
    return False
  type_ = value["type"]
  if type_ == "text":
    return is_valid_text_content(value)
  if type_ == "image":
    return is_valid_image_content(value)
  if type_ == "audio":
    return is_valid_audio_content(value)
  if type_ == "resource_link":
    return is_valid_resource_link(value)
  if type_ == "resource":
    return is_valid_embedded_resource(value)
  # Unknown type: forward-compatible, unless it is a forbidden sampling type.
  return not is_forbidden_content_block_type(type_)
