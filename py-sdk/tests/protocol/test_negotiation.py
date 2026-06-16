"""Tests for revision selection + negotiation errors + the §5.7 probe (§5.4–§5.7)."""

from mcp.protocol.discovery import build_discover_response, DiscoverConfig
from mcp.protocol.negotiation import (
  MISSING_CLIENT_CAPABILITY_CODE,
  NEGOTIATION_ERROR_HTTP_STATUS,
  UNSUPPORTED_PROTOCOL_VERSION_CODE,
  ProtocolSupportCache,
  ProtocolSupportDetermination,
  augment_client_capabilities,
  build_unsupported_protocol_version_error,
  can_satisfy_required_capabilities,
  determination_from_probe,
  http_status_for_negotiation_error,
  interpret_probe_response,
  name_supported_revisions_in_error,
  negotiate_revision,
  reselect_after_unsupported_version,
)

SERVER_INFO = {"name": "s", "version": "1"}


class TestHttpStatus:
  def test_negotiation_codes(self):
    assert http_status_for_negotiation_error(UNSUPPORTED_PROTOCOL_VERSION_CODE) == NEGOTIATION_ERROR_HTTP_STATUS
    assert http_status_for_negotiation_error(MISSING_CLIENT_CAPABILITY_CODE) == NEGOTIATION_ERROR_HTTP_STATUS
    assert http_status_for_negotiation_error(-32601) is None


class TestNegotiateRevision:
  def test_client_order_wins(self):
    r = negotiate_revision(["a", "b"], ["b", "a"])
    assert r.ok and r.selected == "a"

  def test_no_mutual(self):
    r = negotiate_revision(["z"], ["a", "b"])
    assert not r.ok and r.reason == "no-mutual-revision"
    assert r.client_preference == ["z"] and r.server_supported == ["a", "b"]


class TestReselect:
  def test_from_error_supported(self):
    err = build_unsupported_protocol_version_error("z", ["2026-07-28"])
    r = reselect_after_unsupported_version(err, ["2026-07-28"])
    assert r.ok and r.selected == "2026-07-28"

  def test_terminal_when_no_overlap(self):
    err = build_unsupported_protocol_version_error("z", ["a"])
    assert not reselect_after_unsupported_version(err, ["b"]).ok


class TestCapabilityRetry:
  def test_can_satisfy(self):
    assert can_satisfy_required_capabilities({"sampling": {}}, {"sampling": {}, "roots": {}})
    assert not can_satisfy_required_capabilities({"sampling": {}}, {"roots": {}})

  def test_augment(self):
    merged = augment_client_capabilities({"roots": {}}, {"sampling": {}})
    assert merged == {"roots": {}, "sampling": {}}


class TestProbe:
  def test_supported(self):
    resp = build_discover_response(1, DiscoverConfig(["2026-07-28"], {}, SERVER_INFO))
    outcome = interpret_probe_response(resp)
    assert outcome.kind == "supported" and outcome.supported_versions == ["2026-07-28"]

  def test_unsupported_version(self):
    resp = {"jsonrpc": "2.0", "id": 1, "error": build_unsupported_protocol_version_error("z", ["a", "b"])}
    outcome = interpret_probe_response(resp)
    assert outcome.kind == "unsupported-version" and outcome.supported == ["a", "b"] and outcome.requested == "z"

  def test_not_this_protocol(self):
    assert interpret_probe_response(None).kind == "not-this-protocol"
    assert interpret_probe_response({"jsonrpc": "2.0", "id": 1, "error": {"code": -32601, "message": "x"}}).kind == "not-this-protocol"
    assert interpret_probe_response({"jsonrpc": "2.0", "id": 1, "result": {"foo": 1}}).kind == "not-this-protocol"

  def test_determination_from_probe(self):
    resp = build_discover_response(1, DiscoverConfig(["2026-07-28"], {}, SERVER_INFO))
    det = determination_from_probe(interpret_probe_response(resp))
    assert det.speaks_protocol and det.supported_versions == ["2026-07-28"]
    assert not determination_from_probe(interpret_probe_response(None)).speaks_protocol


class TestNameSupportedRevisions:
  def test_annotates_data(self):
    out = name_supported_revisions_in_error({"code": -1, "message": "x"}, ["a", "b"])
    assert out["data"]["supported"] == ["a", "b"]

  def test_preserves_existing_data(self):
    out = name_supported_revisions_in_error({"code": -1, "message": "x", "data": {"k": 1}}, ["a"])
    assert out["data"] == {"k": 1, "supported": ["a"]}


class TestProtocolSupportCache:
  def test_set_get_invalidate(self):
    cache = ProtocolSupportCache()
    det = ProtocolSupportDetermination(True, ["2026-07-28"])
    cache.set("http://x/mcp", det)
    assert cache.has("http://x/mcp") and cache.get("http://x/mcp") == det
    cache.invalidate("http://x/mcp")
    assert not cache.has("http://x/mcp")

  def test_entries_roundtrip(self):
    cache = ProtocolSupportCache()
    cache.set("e", ProtocolSupportDetermination(False))
    rebuilt = ProtocolSupportCache.from_entries(cache.entries())
    assert rebuilt.get("e").speaks_protocol is False
