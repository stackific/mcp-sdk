"""``Annotations`` — optional, untrusted hints on content blocks and resources (§14.6).

Trust model (R-14.6-f/-g): consumers MUST NOT use annotation values for security or
correctness decisions; they are advisory only and MAY influence presentation, ordering,
or context-inclusion.
"""

from __future__ import annotations

from mcp.types.role import is_role


def _is_number(value: object) -> bool:
  return isinstance(value, (int, float)) and not isinstance(value, bool)


def is_valid_annotations(value: object) -> bool:
  """Return ``True`` for a valid ``Annotations`` object (§14.6).

  All fields OPTIONAL (an empty object is valid); extra members are tolerated:

  * ``audience`` — list of ``Role`` values (R-14.6-b);
  * ``priority`` — number in the inclusive range ``0..1`` (R-14.6-c, R-14.6-d);
  * ``lastModified`` — ISO-8601 timestamp string (R-14.6-e).
  """
  if not isinstance(value, dict):
    return False
  if "audience" in value:
    audience = value["audience"]
    if not isinstance(audience, list) or not all(is_role(r) for r in audience):
      return False
  if "priority" in value:
    priority = value["priority"]
    if not _is_number(priority) or not (0 <= priority <= 1):
      return False
  if "lastModified" in value and not isinstance(value["lastModified"], str):
    return False
  return True
