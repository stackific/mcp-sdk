"""Tests for Roots (Deprecated) (§21.1)."""

from mcp.protocol.roots import (
  PROTOCOL_ENFORCES_ROOT_BOUNDARIES,
  ROOTS_LIST_CHANGED_SUPPORTED,
  RootCandidate,
  apply_non_file_disposition,
  assemble_list_roots_result,
  decide_roots_request,
  declares_roots,
  is_conformant_non_file_disposition,
  is_path_traversal_safe,
  is_path_within_reported_roots,
  is_recommended_migration_target,
  is_roots_deprecated,
  is_roots_list_method,
  is_valid_file_uri,
  is_valid_root,
  is_valid_roots_list_input_request,
  is_valid_strict_list_roots_result,
  may_rely_on_roots_list_changed,
  protocol_enforces_root_boundaries,
  should_tolerate_unavailable_root,
)


class TestDeprecation:
  def test_deprecated(self):
    assert is_roots_deprecated()

  def test_migration_targets(self):
    assert is_recommended_migration_target("resource-uris")
    assert not is_recommended_migration_target("roots")


class TestCapability:
  def test_declares(self):
    assert declares_roots({"roots": {}})
    assert not declares_roots({})

  def test_no_list_changed(self):
    assert ROOTS_LIST_CHANGED_SUPPORTED is False
    assert not may_rely_on_roots_list_changed({"roots": {}})

  def test_decide_request(self):
    assert decide_roots_request({"roots": {}}).action == "request"
    assert decide_roots_request({}).action == "proceed-without-roots"


class TestRootsListInputRequest:
  def test_method(self):
    assert is_roots_list_method("roots/list")
    assert not is_roots_list_method("Roots/List")

  def test_input_request(self):
    assert is_valid_roots_list_input_request({"method": "roots/list"})
    assert is_valid_roots_list_input_request({"method": "roots/list", "params": {}})
    assert not is_valid_roots_list_input_request({"method": "other"})


class TestFileUri:
  def test_valid(self):
    assert is_valid_file_uri("file:///home/user")
    assert is_valid_file_uri("file://host/path")

  def test_invalid(self):
    assert not is_valid_file_uri("https://x/y")
    assert not is_valid_file_uri("file:/single-slash")
    assert not is_valid_file_uri("")

  def test_path_traversal(self):
    assert is_path_traversal_safe("file:///home/user")
    assert not is_path_traversal_safe("file:///home/../etc")
    assert not is_path_traversal_safe("file:///home/%2e%2e/etc")


class TestRoot:
  def test_valid(self):
    assert is_valid_root({"uri": "file:///x", "name": "X", "_meta": {}})

  def test_invalid(self):
    assert not is_valid_root({"uri": "https://x"})
    assert not is_valid_root({"name": "X"})
    assert not is_valid_root({"uri": "file:///x", "name": 1})


class TestNonFileDisposition:
  def test_conformant(self):
    assert is_conformant_non_file_disposition("reject")
    assert is_conformant_non_file_disposition("ignore")
    assert not is_conformant_non_file_disposition("keep")

  def test_apply(self):
    assert apply_non_file_disposition("file:///x", "reject")["kept"] is True
    assert apply_non_file_disposition("https://x", "ignore")["kept"] is False


class TestListRootsResult:
  def test_valid(self):
    assert is_valid_strict_list_roots_result({"roots": []})
    assert is_valid_strict_list_roots_result({"roots": [{"uri": "file:///x"}]})

  def test_invalid(self):
    assert not is_valid_strict_list_roots_result({})
    assert not is_valid_strict_list_roots_result({"roots": [{"uri": "https://x"}]})


class TestAssembly:
  def test_includes_qualifying(self):
    out = assemble_list_roots_result([RootCandidate(root={"uri": "file:///ok"}, consented=True, in_scope=True)])
    assert out.result == {"roots": [{"uri": "file:///ok"}]} and out.excluded == []

  def test_excludes_with_reasons(self):
    out = assemble_list_roots_result(
      [
        RootCandidate(root={"uri": "file:///a"}, consented=True, in_scope=False),
        RootCandidate(root={"uri": "file:///b"}, consented=False, in_scope=True),
        RootCandidate(root={"uri": "https://c"}, consented=True, in_scope=True),
        RootCandidate(root={"uri": "file:///d/../e"}, consented=True, in_scope=True),
      ]
    )
    reasons = {e["reason"] for e in out.excluded}
    assert reasons == {"not-in-scope", "no-consent", "invalid-uri", "path-traversal"}
    assert out.result == {"roots": []}


class TestServerSide:
  def test_non_enforcement(self):
    assert PROTOCOL_ENFORCES_ROOT_BOUNDARIES is False
    assert not protocol_enforces_root_boundaries()

  def test_tolerate_unavailable(self):
    assert should_tolerate_unavailable_root({"uri": "file:///gone"})

  def test_path_within_roots(self):
    roots = [{"uri": "file:///home/user"}]
    assert is_path_within_reported_roots("file:///home/user/doc.txt", roots)
    assert is_path_within_reported_roots("file:///home/user", roots)
    assert not is_path_within_reported_roots("file:///etc/passwd", roots)
    assert not is_path_within_reported_roots("file:///home/userdata", roots)  # not a path-segment prefix
