"""Roots (Deprecated) (§21.1).

⚠️ DEPRECATED capability — defined only for interoperability; new functionality SHOULD
convey directories/files via tool inputs, resource URIs, or server config. Roots let a
client expose filesystem "roots" (informational guidance, NOT an access boundary).
Delivered via the §11 multi-round-trip ``roots/list`` input request (the envelope is owned
by that module). This port fills in the §21.1 pieces: capability shape, the ``Root`` entry
(``file://`` + RFC3986 + path-traversal guard), non-``file`` disposition, client assembly
(consent/scope), and server non-enforcement + path-containment validation.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from urllib.parse import unquote, urlsplit

from mcp.protocol.capability_negotiation import (
  client_declares,
  is_deprecated_client_capability,
  may_invoke_roots_list,
)

ROOTS_CAPABILITY_NAME = "roots"
ROOTS_MIGRATION_TARGETS = ("tool-input-parameters", "resource-uris", "server-configuration")


def is_roots_deprecated() -> bool:
  """Return ``True`` — the ``roots`` capability is Deprecated in this revision. (R-21.1.1-a)"""
  return is_deprecated_client_capability(ROOTS_CAPABILITY_NAME)


def is_recommended_migration_target(target: str) -> bool:
  """Return ``True`` for a migration mechanism to adopt instead of roots (``"roots"`` is not
  a member). (R-21.1.1-b)
  """
  return target in ROOTS_MIGRATION_TARGETS


# ─── roots capability value (§21.1.2) ─────────────────────────────────────────

def is_valid_roots_capability_value(value: object) -> bool:
  """Return ``True`` for a valid ``roots`` capability value: any JSON object (canonically
  ``{}``); non-object values are rejected, unknown members tolerated. (R-21.1.2-a/-b)
  """
  return isinstance(value, dict)


def declares_roots(caps: dict) -> bool:
  """Return ``True`` when client capabilities declare the (Deprecated) ``roots``. (R-21.1.2-a)"""
  return client_declares(caps, ROOTS_CAPABILITY_NAME)


#: No listChanged sub-flag is defined for roots in this revision. (R-21.1.2-c)
ROOTS_LIST_CHANGED_SUPPORTED = False
ROOTS_LIST_CHANGED_NOTIFICATION_METHOD = "notifications/roots/list_changed"


def may_rely_on_roots_list_changed(_client_caps: dict) -> bool:
  """Return ``False`` always — a client MUST NOT rely on a listChanged mechanism for roots.
  (R-21.1.2-c)
  """
  return ROOTS_LIST_CHANGED_SUPPORTED


# ─── server-side gating (§21.1.2) ─────────────────────────────────────────────

@dataclass(frozen=True)
class RootsRequestDecision:
  """Outcome of :func:`decide_roots_request`: ``"request"`` or ``"proceed-without-roots"``."""

  action: str


def decide_roots_request(client_caps: dict) -> RootsRequestDecision:
  """Decide whether a server may request roots: ``request`` when the client declared
  ``roots``, else ``proceed-without-roots``. (R-21.1.2-d/-e)
  """
  return RootsRequestDecision("request" if may_invoke_roots_list(client_caps) else "proceed-without-roots")


# ─── roots/list input request (§21.1.4) ───────────────────────────────────────

ROOTS_LIST_METHOD = "roots/list"


def is_roots_list_method(value: object) -> bool:
  """Return ``True`` when ``value`` is exactly ``"roots/list"`` (case-sensitive). (R-21.1.4-a)"""
  return value == ROOTS_LIST_METHOD


def is_valid_roots_list_input_request(value: object) -> bool:
  """Return ``True`` for a ``roots/list`` input request: ``method == "roots/list"``; OPTIONAL
  object ``params`` (absence tolerated). The full MRTR envelope is owned by the
  multi-round-trip module. (§21.1.4, R-21.1.4-a/-b/-c)
  """
  if not isinstance(value, dict) or value.get("method") != ROOTS_LIST_METHOD:
    return False
  return "params" not in value or isinstance(value["params"], dict)


# ─── the Root entry (§21.1.5) ─────────────────────────────────────────────────

#: Characters permitted anywhere in an RFC 3986 URI (unreserved + reserved + ``%`` for
#: pct-encoding). A char outside this set (space, raw ``\``, control char, …) means the
#: string is NOT a syntactically valid URI. Mirrors the WHATWG ``URL`` parser's rejection
#: of such inputs (the TS SDK uses ``new URL(uri)`` for the RFC 3986 check). (R-21.1.5-d)
_RFC3986_CHARS = re.compile(r"^[A-Za-z0-9\-._~:/?#\[\]@!$&'()*+,;=%]*$")
#: A ``%`` NOT followed by exactly two hex digits — a malformed percent-escape. (R-21.1.5-d)
_BAD_PCT_ENCODING = re.compile(r"%(?![0-9A-Fa-f]{2})")


def _is_rfc3986_syntactically_valid(uri: str) -> bool:
  """Return ``True`` when ``uri`` contains only RFC 3986-permitted characters and every
  percent-escape is well-formed (``%`` followed by two hex digits). This is the RFC 3986
  syntactic-validity gate the TS SDK gets from the WHATWG ``URL`` parser (which throws on
  spaces, raw backslashes, control chars, and bad ``%`` escapes); ``urllib`` is lenient and
  would otherwise accept such malformed inputs. (R-21.1.5-d)
  """
  if not _RFC3986_CHARS.match(uri):
    return False
  return _BAD_PCT_ENCODING.search(uri) is None


def is_valid_file_uri(uri: object) -> bool:
  """Return ``True`` when ``uri`` is a syntactically valid absolute URI per RFC 3986 AND
  uses the ``file`` scheme (begins with ``file://``). A non-``file`` scheme, a missing/empty
  value, or a malformed URI (spaces, raw ``\\``, stray ``%``, control chars) all return
  ``False``. (R-21.1.5-b/-d)
  """
  if not isinstance(uri, str) or uri == "" or not uri.startswith("file://"):
    return False
  if not _is_rfc3986_syntactically_valid(uri):
    return False
  try:
    return urlsplit(uri).scheme == "file"
  except ValueError:
    return False


def is_path_traversal_safe(uri: object) -> bool:
  """Return ``True`` when a valid ``file://`` ``uri`` shows no path-traversal artifacts — no
  ``..`` segment and no percent-encoded ``..``. Inspects the RAW path (the URL parser would
  collapse ``..``). (R-21.1.5-i)
  """
  if not is_valid_file_uri(uri):
    return False
  after = uri[len("file://") :]
  first_slash = after.find("/")
  if first_slash == -1:
    return True
  raw_path = after[first_slash:]
  for segment in raw_path.split("/"):
    if unquote(segment) == "..":
      return False
  return True


def is_valid_root(value: object) -> bool:
  """Return ``True`` for a valid ``Root`` (§21.1.5): REQUIRED ``file://`` ``uri``; OPTIONAL
  string ``name`` and object ``_meta`` (unknown members tolerated). (R-21.1.5-b/-d/-e/-f)
  """
  if not isinstance(value, dict) or not is_valid_file_uri(value.get("uri")):
    return False
  if "name" in value and not isinstance(value["name"], str):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


# ─── non-file-scheme handling (§21.1.5) ───────────────────────────────────────

def is_conformant_non_file_disposition(disposition: str) -> bool:
  """Return ``True`` when ``disposition`` is a conformant non-``file`` handling: ``"reject"``
  or ``"ignore"``. (R-21.1.5-c)
  """
  return disposition in ("reject", "ignore")


def apply_non_file_disposition(uri: object, disposition: str) -> dict:
  """Apply a disposition to a candidate ``uri`` not using the ``file`` scheme. A ``file://``
  URI is kept; a non-``file`` URI is dropped under either disposition. (R-21.1.5-c)
  """
  if is_valid_file_uri(uri):
    return {"kept": True, "disposition": disposition}
  return {"kept": False, "disposition": disposition}


# ─── ListRootsResult (§21.1.5) ────────────────────────────────────────────────

def is_valid_strict_list_roots_result(value: object) -> bool:
  """Return ``True`` for a ``ListRootsResult`` with full §21.1 ``Root`` enforcement: REQUIRED
  ``roots`` array (MAY be empty) of valid ``file://`` Roots. (R-21.1.5-a/-b/-d)
  """
  if not isinstance(value, dict):
    return False
  roots = value.get("roots")
  return isinstance(roots, list) and all(is_valid_root(r) for r in roots)


# ─── client-side assembly (§21.1.5) ───────────────────────────────────────────

@dataclass(frozen=True)
class RootCandidate:
  """A candidate root + its consent/scope state."""

  root: dict
  consented: bool
  in_scope: bool


@dataclass(frozen=True)
class RootsAssembly:
  """Outcome of :func:`assemble_list_roots_result`: the validated listing + exclusions."""

  result: dict
  excluded: list = field(default_factory=list)


def assemble_list_roots_result(candidates: list[RootCandidate]) -> RootsAssembly:
  """Assemble a ``ListRootsResult`` enforcing the client-side obligations: a root is included
  only when in-scope AND consented AND URI-valid AND traversal-safe; excluded candidates are
  reported with a reason. No qualifier → the conformant empty ``{roots: []}``. (§21.1.5)
  """
  included: list[dict] = []
  excluded: list[dict] = []
  for candidate in candidates:
    if not candidate.in_scope:
      excluded.append({"root": candidate.root, "reason": "not-in-scope"})
    elif not candidate.consented:
      excluded.append({"root": candidate.root, "reason": "no-consent"})
    elif not is_valid_file_uri(candidate.root.get("uri")):
      excluded.append({"root": candidate.root, "reason": "invalid-uri"})
    elif not is_path_traversal_safe(candidate.root.get("uri")):
      excluded.append({"root": candidate.root, "reason": "path-traversal"})
    else:
      included.append(candidate.root)
  return RootsAssembly(result={"roots": included}, excluded=excluded)


# ─── server-side: non-enforcement & path containment (§21.1.5) ────────────────

#: The protocol does NOT enforce root boundaries — roots are guidance. (R-21.1.5-l)
PROTOCOL_ENFORCES_ROOT_BOUNDARIES = False


def protocol_enforces_root_boundaries() -> bool:
  """Return ``False`` — a server MUST validate derived paths itself, not assume enforcement.
  (R-21.1.5-l)
  """
  return PROTOCOL_ENFORCES_ROOT_BOUNDARIES


def should_tolerate_unavailable_root(_root: dict) -> bool:
  """Return ``True`` — a server SHOULD tolerate a previously-reported root that is now
  unavailable rather than failing. (R-21.1.5-j)
  """
  return True


def _decoded_segments(pathname: str) -> list[str]:
  return [unquote(s) for s in pathname.split("/") if s]


def _is_prefix_path(prefix: list[str], path: list[str]) -> bool:
  if len(prefix) > len(path):
    return False
  return all(prefix[i] == path[i] for i in range(len(prefix)))


def is_path_within_reported_roots(derived_uri: object, reported_roots: list[dict]) -> bool:
  """Return ``True`` when ``derived_uri`` is a valid ``file://`` URI contained within (equal
  to or descended from) at least one reported root's path. Compares decoded path segments.
  (R-21.1.5-k/-l)
  """
  if not is_valid_file_uri(derived_uri):
    return False
  derived_segments = _decoded_segments(urlsplit(derived_uri).path)
  for root in reported_roots:
    if not is_valid_file_uri(root.get("uri")):
      continue
    root_segments = _decoded_segments(urlsplit(root["uri"]).path)
    if _is_prefix_path(root_segments, derived_segments):
      return True
  return False
