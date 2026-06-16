"""Resources I — capability, listing, templates & types (§17.1–§17.4).

The discovery surface for resources (server-provided units of context). Fixes the
``resources`` capability (``listChanged``/``subscribe`` sub-flags) + gating, the
``Resource`` and ``ResourceTemplate`` types (with RFC3986 URI and RFC6570 URI-Template
validation), and the paginated/cacheable ``resources/list`` +
``resources/templates/list`` results. Reading is in :mod:`mcp.protocol.resources_read`.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from mcp.protocol.capability_negotiation import client_should_expect_notification, server_declares
from mcp.types.annotations import is_valid_annotations
from mcp.types.base_metadata import is_valid_base_metadata, resolve_display_name

# Method + notification names (the notification names are owned by the streaming
# module in the TS SDK; pinned here as literals to avoid a forward dependency).
RESOURCES_LIST_METHOD = "resources/list"
RESOURCES_TEMPLATES_LIST_METHOD = "resources/templates/list"
RESOURCES_LIST_CHANGED_METHOD = "notifications/resources/list_changed"
RESOURCES_UPDATED_METHOD = "notifications/resources/updated"

#: The three requests gated by the ``resources`` capability. (§17.1)
RESOURCE_GATED_METHODS = (RESOURCES_LIST_METHOD, RESOURCES_TEMPLATES_LIST_METHOD, "resources/read")


# ─── Capability + gating (§17.1) ──────────────────────────────────────────────

def is_valid_resources_capability(value: object) -> bool:
  """Return ``True`` for a valid ``resources`` capability: OPTIONAL boolean
  ``listChanged``/``subscribe``; empty ``{}`` valid. (§17.1)
  """
  if not isinstance(value, dict):
    return False
  for key in ("listChanged", "subscribe"):
    if key in value and not isinstance(value[key], bool):
      return False
  return True


def server_declares_resources(server_caps: dict) -> bool:
  """Return ``True`` when the server declares the ``resources`` capability. (§17.1, R-17.1-h)"""
  return server_declares(server_caps, "resources")


def may_accept_resource_request(method: str, server_caps: dict) -> bool:
  """Return ``True`` when a server MAY accept resource request ``method`` — it is gated
  AND ``resources`` is declared. (§17.1, R-17.1-h)
  """
  if method not in RESOURCE_GATED_METHODS:
    return False
  return server_declares_resources(server_caps)


def client_may_issue_resource_request(method: str, server_caps: dict) -> bool:
  """Client-side mirror of :func:`may_accept_resource_request`. (§17.1, R-17.1-j)"""
  return may_accept_resource_request(method, server_caps)


def may_emit_resources_list_changed(server_caps: dict) -> bool:
  """Return ``True`` when the server MAY emit ``notifications/resources/list_changed``
  (needs ``resources`` + ``listChanged``). (§17.1, R-17.1-i/-k)
  """
  return client_should_expect_notification(RESOURCES_LIST_CHANGED_METHOD, server_caps)


def may_emit_resource_updated(server_caps: dict) -> bool:
  """Return ``True`` when the server MAY emit ``notifications/resources/updated`` (needs
  ``resources`` + ``subscribe``). (§17.1, R-17.1-i/-l)
  """
  return client_should_expect_notification(RESOURCES_UPDATED_METHOD, server_caps)


# ─── URI validation (§17.4, RFC3986) ──────────────────────────────────────────

_URI_SCHEME_RE = re.compile(r"^[a-zA-Z][a-zA-Z0-9+.\-]*:")


def is_resource_uri(value: object) -> bool:
  """Return ``True`` when ``value`` is an absolute RFC3986 URI usable as a ``Resource.uri``
  — a conformant scheme plus at least one further character. A relative reference (no
  scheme) is rejected. (§17.4, R-17.4-a/-b)
  """
  if not isinstance(value, str) or value == "":
    return False
  if not _URI_SCHEME_RE.match(value):
    return False
  after = value.split(":", 1)
  return len(after) == 2 and after[1] != ""


# ─── URI-template validation (§17.4, RFC6570) ─────────────────────────────────

_URI_TEMPLATE_OPERATOR = "+#./;?&"
_VARNAME_RE = re.compile(r"^(?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2})+(?:\.(?:[A-Za-z0-9_]|%[0-9A-Fa-f]{2})+)*$")
_PREFIX_LEN_RE = re.compile(r"^[1-9]\d{0,3}$")


def _is_valid_varname(name: str) -> bool:
  return bool(name) and bool(_VARNAME_RE.match(name))


def _is_valid_varspec(spec: str) -> bool:
  if not spec:
    return False
  if spec.endswith("*"):
    return _is_valid_varname(spec[:-1])
  colon = spec.find(":")
  if colon != -1:
    name, length = spec[:colon], spec[colon + 1 :]
    return bool(_PREFIX_LEN_RE.match(length)) and _is_valid_varname(name)
  return _is_valid_varname(spec)


def is_uri_template(value: object) -> bool:
  """Return ``True`` when ``value`` conforms to the RFC6570 URI Template grammar: literal
  characters interspersed with well-formed ``{…}`` expressions. (§17.4, R-17.4-m)
  """
  if not isinstance(value, str) or value == "":
    return False
  i = 0
  while i < len(value):
    ch = value[i]
    if ch == "}":
      return False
    if ch != "{":
      i += 1
      continue
    close = value.find("}", i + 1)
    if close == -1:
      return False
    body = value[i + 1 : close]
    if body == "" or "{" in body:
      return False
    if body[0] in _URI_TEMPLATE_OPERATOR:
      body = body[1:]
      if body == "":
        return False
    if not all(_is_valid_varspec(spec) for spec in body.split(",")):
      return False
    i = close + 1
  return True


def uri_template_variables(template: str) -> list[str]:
  """Extract the variable names referenced by a template's ``{…}`` expressions, in
  first-seen order, modifiers + operator stripped. (§17.4, R-17.4-n)
  """
  names: list[str] = []
  seen: set[str] = set()
  for match in re.finditer(r"\{([^{}]+)\}", template):
    body = match.group(1)
    if body[0] in _URI_TEMPLATE_OPERATOR:
      body = body[1:]
    for spec in body.split(","):
      name = re.sub(r"\*.*$", "", spec)
      name = re.sub(r":.*$", "", name)
      if name and name not in seen:
        seen.add(name)
        names.append(name)
  return names


# ─── Resource / ResourceTemplate types (§17.4) ────────────────────────────────

def _is_number(value: object) -> bool:
  return isinstance(value, (int, float)) and not isinstance(value, bool)


def _common_descriptor_ok(value: dict) -> bool:
  for key in ("description", "mimeType"):
    if key in value and not isinstance(value[key], str):
      return False
  if "annotations" in value and not is_valid_annotations(value["annotations"]):
    return False
  if "icons" in value and not isinstance(value["icons"], list):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


def is_valid_resource(value: object) -> bool:
  """Return ``True`` for a well-formed ``Resource`` (§17.4): ``BaseMetadata`` + REQUIRED
  RFC3986 ``uri``; OPTIONAL ``description``/``mimeType``/``size``/``annotations``/``icons``/``_meta``.
  """
  if not is_valid_base_metadata(value) or not is_resource_uri(value.get("uri")):
    return False
  if "size" in value and not _is_number(value["size"]):
    return False
  return _common_descriptor_ok(value)


def is_valid_resource_template(value: object) -> bool:
  """Return ``True`` for a well-formed ``ResourceTemplate`` (§17.4): ``BaseMetadata`` +
  REQUIRED RFC6570 ``uriTemplate``; same OPTIONAL fields as ``Resource`` minus ``size``.
  """
  if not is_valid_base_metadata(value) or not is_uri_template(value.get("uriTemplate")):
    return False
  return _common_descriptor_ok(value)


def resource_template_has_no_size(template: dict) -> bool:
  """Return ``True`` when ``template`` carries no ``size`` field — a ``ResourceTemplate``
  MUST NOT have one. (§17.4, R-17.4-u)
  """
  return "size" not in template


def resource_display_name(resource: dict) -> str:
  """User-facing label for a ``Resource``: prefer ``title``, fall back to ``name``. (R-17.4-e)"""
  return resolve_display_name(resource["name"], resource.get("title"))


def resource_template_display_name(template: dict) -> str:
  """User-facing label for a ``ResourceTemplate``: prefer ``title``, fall back to ``name``."""
  return resolve_display_name(template["name"], template.get("title"))


# ─── list results (§17.2, §17.3) ──────────────────────────────────────────────

@dataclass(frozen=True)
class ListCacheHints:
  """The REQUIRED caching hints every list result carries together. (§13)"""

  ttl_ms: int
  cache_scope: str


def _is_valid_list_result(value: object, key: str, item_predicate) -> bool:
  if not isinstance(value, dict) or value.get("resultType") != "complete":
    return False
  items = value.get(key)
  if not isinstance(items, list) or not all(item_predicate(i) for i in items):
    return False
  if "nextCursor" in value and not isinstance(value["nextCursor"], str):
    return False
  ttl = value.get("ttlMs")
  if not isinstance(ttl, int) or isinstance(ttl, bool) or ttl < 0:
    return False
  if value.get("cacheScope") not in ("public", "private"):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


def is_valid_list_resources_result(value: object) -> bool:
  """Return ``True`` for a well-formed ``ListResourcesResult`` (§17.2)."""
  return _is_valid_list_result(value, "resources", is_valid_resource)


def is_valid_list_resource_templates_result(value: object) -> bool:
  """Return ``True`` for a well-formed ``ListResourceTemplatesResult`` (§17.3)."""
  return _is_valid_list_result(value, "resourceTemplates", is_valid_resource_template)


def _build_list_result(key: str, items: list, hints: ListCacheHints, next_cursor: str | None, meta: dict | None) -> dict:
  if hints.ttl_ms < 0:
    raise ValueError("list result ttlMs MUST be >= 0 (R-17.2-g)")
  result: dict = {"resultType": "complete", key: list(items), "ttlMs": hints.ttl_ms, "cacheScope": hints.cache_scope}
  if next_cursor is not None:
    result["nextCursor"] = next_cursor
  if meta is not None:
    result["_meta"] = meta
  return result


def build_list_resources_result(resources: list, hints: ListCacheHints, *, next_cursor: str | None = None, meta: dict | None = None) -> dict:
  """Build a ``ListResourcesResult`` (``resultType: "complete"`` + caching hints). (§17.2)

  :raises ValueError: when ``hints.ttl_ms`` is negative.
  """
  return _build_list_result("resources", resources, hints, next_cursor, meta)


def build_list_resource_templates_result(templates: list, hints: ListCacheHints, *, next_cursor: str | None = None, meta: dict | None = None) -> dict:
  """Build a ``ListResourceTemplatesResult``. (§17.3)

  :raises ValueError: when ``hints.ttl_ms`` is negative.
  """
  return _build_list_result("resourceTemplates", templates, hints, next_cursor, meta)


# ─── capability declaration helpers (§17.1) ───────────────────────────────────

def build_resources_capability(*, list_changed: bool = False, subscribe: bool = False) -> dict:
  """Build the ``resources`` capability value, including a sub-flag only when ``True``.
  (§17.1, R-17.1-f/-g)
  """
  cap: dict = {}
  if list_changed is True:
    cap["listChanged"] = True
  if subscribe is True:
    cap["subscribe"] = True
  return cap


def get_resources_capability(caps: dict) -> dict | None:
  """Return the ``resources`` capability object from ``ServerCapabilities``, or ``None``."""
  value = caps.get("resources")
  return value if isinstance(value, dict) else None
