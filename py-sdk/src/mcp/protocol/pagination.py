"""Cursor-based pagination (§12).

Utilities for the uniform page-by-page exchange: cursor-presence / end-of-results
predicates, the per-cursor cache-key helper, the invalid-cursor (-32602) error, and a
reference offset paginator. The ``Cursor`` type is the opaque string from §3.7.

Paginated methods: ``tools/list``, ``resources/list``, ``resources/templates/list``,
``prompts/list``.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field

# Error code for an invalid / unrecognized cursor. (R-12.4-c) — same value as -32602.
INVALID_CURSOR_CODE = -32602


def has_next_cursor(result: dict) -> bool:
  """Return ``True`` when ``nextCursor`` is present (even ``""``), i.e. more may follow.

  The empty string is a PRESENT cursor — only absence signals the last page.
  (R-12.2-c, R-12.3-d)
  """
  return "nextCursor" in result


def is_last_page(result: dict) -> bool:
  """Return ``True`` when this is the final page — ``nextCursor`` is absent. (R-12.2-d)"""
  return not has_next_cursor(result)


def is_cursor_present(cursor: str | None) -> bool:
  """Return ``True`` when ``cursor`` is a present value (including ``""``). Only ``None``
  signals "no cursor". (R-12.1-a)
  """
  return cursor is not None


def build_invalid_cursor_error(message: str | None = None) -> dict:
  """Build the JSON-RPC error payload for an invalid-cursor rejection. (R-12.4-c/-d)"""
  return {"code": INVALID_CURSOR_CODE, "message": message or "Invalid params: unrecognized cursor"}


def pagination_cache_key(method: str, cursor: str | None) -> str:
  """Produce a per-page cache key. A page cached for one ``cursor`` MUST NOT be served
  for a request bearing a different ``cursor`` (including the first-page request which
  omits it). (R-12.5-a, R-13.5-i)
  """
  return f"{method}::page:first" if cursor is None else f"{method}::page:cursor:{cursor}"


@dataclass(frozen=True)
class PaginatorPage:
  """Outcome of :meth:`OffsetPaginator.get_page`.

  On success ``ok`` is ``True`` with ``items`` + ``next_cursor``; on an unrecognized
  cursor ``ok`` is ``False`` with a structured ``error`` (never raised).
  """

  ok: bool
  items: list = field(default_factory=list)
  next_cursor: str | None = None
  error: dict | None = None


class OffsetPaginator:
  """Reference cursor paginator over an in-memory list. Cursors are deterministic decimal
  offset strings (stable); an unrecognized cursor yields a structured error, never raises.
  """

  def __init__(self, items: list, page_size: int = 20) -> None:
    if not isinstance(page_size, int) or page_size < 1:
      raise ValueError("page_size must be a positive integer")
    self._items = list(items)
    self.page_size = page_size

  def get_page(self, cursor: str | None) -> PaginatorPage:
    """Return a page for ``cursor`` (absent → first page); unrecognized → error result."""
    offset = 0 if cursor is None else self._decode_cursor(cursor)
    if offset is None:
      return PaginatorPage(False, error=build_invalid_cursor_error())
    page = self._items[offset : offset + self.page_size]
    next_offset = offset + self.page_size
    next_cursor = str(next_offset) if next_offset < len(self._items) else None
    return PaginatorPage(True, items=list(page), next_cursor=next_cursor)

  def _decode_cursor(self, cursor: str) -> int | None:
    if not re.match(r"^\d+$", cursor):
      return None
    n = int(cursor)
    return n if 0 <= n <= len(self._items) else None


#: Method names whose results carry paginated shapes. (§12)
PAGINATED_METHODS = frozenset({"tools/list", "resources/list", "resources/templates/list", "prompts/list"})


def is_paginated_method(method: str) -> bool:
  """Return ``True`` when ``method`` is one of the paginated list methods."""
  return method in PAGINATED_METHODS
