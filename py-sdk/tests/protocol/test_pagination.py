"""Tests for cursor-based pagination (§12)."""

from mcp.protocol.pagination import (
  INVALID_CURSOR_CODE,
  OffsetPaginator,
  build_invalid_cursor_error,
  has_next_cursor,
  is_cursor_present,
  is_last_page,
  is_paginated_method,
  pagination_cache_key,
)


class TestCursorPredicates:
  def test_present_including_empty_string(self):
    assert has_next_cursor({"nextCursor": "abc"})
    assert has_next_cursor({"nextCursor": ""})  # empty string is PRESENT
    assert not has_next_cursor({})

  def test_last_page(self):
    assert is_last_page({})
    assert not is_last_page({"nextCursor": ""})

  def test_is_cursor_present(self):
    assert is_cursor_present("")
    assert is_cursor_present("x")
    assert not is_cursor_present(None)


class TestInvalidCursorError:
  def test_shape(self):
    err = build_invalid_cursor_error()
    assert err["code"] == INVALID_CURSOR_CODE
    assert build_invalid_cursor_error("nope")["message"] == "nope"


class TestCacheKey:
  def test_first_vs_cursor(self):
    assert pagination_cache_key("tools/list", None) == "tools/list::page:first"
    assert pagination_cache_key("tools/list", "5") == "tools/list::page:cursor:5"

  def test_first_and_empty_cursor_differ(self):
    assert pagination_cache_key("m", None) != pagination_cache_key("m", "")


class TestOffsetPaginator:
  def test_first_page_and_next_cursor(self):
    p = OffsetPaginator(list(range(5)), page_size=2)
    page = p.get_page(None)
    assert page.ok and page.items == [0, 1] and page.next_cursor == "2"

  def test_follow_cursor(self):
    p = OffsetPaginator(list(range(5)), page_size=2)
    page = p.get_page("2")
    assert page.items == [2, 3] and page.next_cursor == "4"

  def test_last_page_has_no_cursor(self):
    p = OffsetPaginator(list(range(4)), page_size=2)
    assert p.get_page("2").next_cursor is None

  def test_invalid_cursor_is_error_not_raise(self):
    p = OffsetPaginator([1, 2, 3], page_size=2)
    page = p.get_page("notanumber")
    assert not page.ok and page.error["code"] == INVALID_CURSOR_CODE

  def test_out_of_range_cursor_rejected(self):
    p = OffsetPaginator([1, 2, 3], page_size=2)
    assert not p.get_page("99").ok

  def test_bad_page_size(self):
    import pytest

    with pytest.raises(ValueError):
      OffsetPaginator([], page_size=0)


class TestPaginatedMethods:
  def test_membership(self):
    assert is_paginated_method("tools/list")
    assert is_paginated_method("resources/templates/list")
    assert not is_paginated_method("tools/call")
