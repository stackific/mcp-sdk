"""Tests for response caching hints + ResponseCache (§13)."""

from mcp.protocol.caching import (
  ResponseCache,
  effective_cache_scope,
  expires_at,
  has_both_or_neither_cache_hints,
  has_consistent_cache_scope,
  is_cache_hint_valid,
  is_cacheable_method,
  is_fresh,
  methods_for_notification,
  resolve_cache_scope,
)


class TestHintValidation:
  def test_valid(self):
    assert is_cache_hint_valid(0, "private")
    assert is_cache_hint_valid(5000, "public")

  def test_invalid(self):
    assert not is_cache_hint_valid(-1, "public")
    assert not is_cache_hint_valid(1.5, "public")
    assert not is_cache_hint_valid(True, "public")  # bool is not an int ttl
    assert not is_cache_hint_valid(0, "secret")

  def test_both_or_neither(self):
    assert has_both_or_neither_cache_hints({"ttlMs": 0, "cacheScope": "private"})
    assert has_both_or_neither_cache_hints({})
    assert not has_both_or_neither_cache_hints({"ttlMs": 0})


class TestScopeResolution:
  def test_resolve(self):
    assert resolve_cache_scope("public") == "public"
    assert resolve_cache_scope("private") == "private"
    assert resolve_cache_scope("weird") == "private"
    assert resolve_cache_scope(None) == "private"

  def test_consistency(self):
    assert has_consistent_cache_scope([])
    assert has_consistent_cache_scope(["public", "public"])
    assert not has_consistent_cache_scope(["public", "private"])

  def test_effective(self):
    assert effective_cache_scope(["public", "public"]) == "public"
    assert effective_cache_scope(["public", "private"]) == "private"


class TestFreshness:
  def test_is_fresh(self):
    assert is_fresh(1000, received_at=100, now=500)
    assert not is_fresh(1000, received_at=100, now=1200)
    assert not is_fresh(0, received_at=100, now=100)  # ttl 0 is never fresh

  def test_expires_at(self):
    assert expires_at(1000, 100) == 1100


class TestNotifications:
  def test_methods_for_notification(self):
    assert set(methods_for_notification("notifications/resources/list_changed")) == {
      "resources/list",
      "resources/templates/list",
    }
    assert methods_for_notification("notifications/tools/list_changed") == ["tools/list"]

  def test_cacheable_methods(self):
    assert is_cacheable_method("resources/read")
    assert not is_cacheable_method("tools/call")


class TestResponseCache:
  def test_set_get_hit(self):
    cache = ResponseCache()
    cache.set("k", {"ttlMs": 1000, "cacheScope": "public", "x": 1}, received_at=0)
    hit = cache.get("k", now=500)
    assert hit.hit and hit.value["x"] == 1 and hit.cache_scope == "public"

  def test_stale_is_miss_and_evicted(self):
    cache = ResponseCache()
    cache.set("k", {"ttlMs": 100, "cacheScope": "private"}, received_at=0)
    assert not cache.get("k", now=1000).hit
    assert cache.size == 0  # evicted

  def test_ttl_zero_stored_but_never_fresh(self):
    cache = ResponseCache()
    cache.set("k", {"ttlMs": 0, "cacheScope": "private"}, received_at=0)
    assert cache.size == 1
    assert not cache.get("k", now=0).hit

  def test_invalid_hint_not_stored(self):
    cache = ResponseCache()
    cache.set("k", {"ttlMs": -1, "cacheScope": "public"}, received_at=0)
    assert cache.size == 0

  def test_invalidate_by_notification_evicts_pages(self):
    cache = ResponseCache()
    cache.set("tools/list", {"ttlMs": 1000, "cacheScope": "public"}, 0)
    cache.set("tools/list::page:cursor:2", {"ttlMs": 1000, "cacheScope": "public"}, 0)
    cache.set("prompts/list", {"ttlMs": 1000, "cacheScope": "public"}, 0)
    cache.invalidate_by_notification("notifications/tools/list_changed")
    assert cache.size == 1  # only prompts/list remains
