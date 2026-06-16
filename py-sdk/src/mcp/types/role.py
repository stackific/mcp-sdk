"""``Role`` — the two-value conversation-participant enumeration (§14.7).

Used by ``Annotations.audience`` and by prompt messages. The set is closed: only
``"user"`` and ``"assistant"`` are valid. (R-14.7-a)
"""

from __future__ import annotations

#: The closed set of conversation roles. (§14.7, R-14.7-a)
ROLES = frozenset({"user", "assistant"})


def is_role(value: object) -> bool:
  """Return ``True`` when ``value`` is a valid ``Role`` (``"user"`` or ``"assistant"``)."""
  return value in ROLES
