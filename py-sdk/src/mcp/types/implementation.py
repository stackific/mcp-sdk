"""The ``Implementation`` descriptor (§14.3).

``Implementation`` identifies a piece of MCP software (a client or a server). It
composes ``BaseMetadata`` (name/title) and ``Icons`` and adds ``version`` (REQUIRED),
``description`` and ``websiteUrl`` (both OPTIONAL).

Required: ``name``, ``version``. Optional: ``title``, ``icons``, ``description``,
``websiteUrl``. Unknown properties are tolerated (the §2.3.4 forward-compatibility
rule) — captured here in :attr:`Implementation.extra`.

Wire examples::

    {"name": "example-client", "version": "0.1.0"}
    {"name": "example-server", "title": "Example MCP Server", "version": "2.4.1",
     "description": "Provides filesystem and search tools.",
     "websiteUrl": "https://example.com/mcp"}
"""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass(frozen=True)
class Implementation:
  """A parsed ``Implementation`` descriptor (§14.3, R-14.3-a – R-14.3-f).

  Only the spec-defined fields are surfaced as attributes; any additional members are
  preserved in :attr:`extra` so the forward-compatibility rule (§2.3.4) holds.
  """

  name: str
  version: str
  title: str | None = None
  description: str | None = None
  website_url: str | None = None
  icons: list[dict] | None = None
  extra: dict = field(default_factory=dict)


#: The spec-defined keys, used to separate known fields from forward-compatible extras.
_KNOWN_KEYS = {"name", "title", "icons", "version", "description", "websiteUrl"}


def is_valid_implementation(value: object) -> bool:
  """Return ``True`` when ``value`` is a valid ``Implementation``.

  REQUIRED: string ``name`` and string ``version``. Other fields are not validated
  here (they are optional and forward-compatible). (R-14.3-a, R-14.3-d)
  """
  if not isinstance(value, dict):
    return False
  return isinstance(value.get("name"), str) and isinstance(value.get("version"), str)


def parse_implementation(value: object) -> Implementation:
  """Parse and validate an ``Implementation`` descriptor.

  Unknown properties are preserved in :attr:`Implementation.extra` (§2.3.4).

  :raises ValueError: when ``name`` or ``version`` is absent or not a string.
  """
  if not is_valid_implementation(value):
    raise ValueError("Implementation requires string `name` and `version` (§14.3)")
  assert isinstance(value, dict)
  extra = {k: v for k, v in value.items() if k not in _KNOWN_KEYS}
  return Implementation(
    name=value["name"],
    version=value["version"],
    title=value.get("title"),
    description=value.get("description"),
    website_url=value.get("websiteUrl"),
    icons=value.get("icons"),
    extra=extra,
  )
