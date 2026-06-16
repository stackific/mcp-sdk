"""Tests for JSON-RPC method dispatch (§3.3)."""

from mcp.jsonrpc.dispatch import MethodDescriptor, dispatch_request


def _request(method: str, **extra) -> dict:
  return {"jsonrpc": "2.0", "id": 1, "method": method, **extra}


class TestDispatch:
  def test_method_found_no_validation(self):
    registry = {"ping": MethodDescriptor()}
    outcome = dispatch_request(_request("ping"), registry)
    assert outcome.ok and outcome.response is None

  def test_method_not_found(self):
    outcome = dispatch_request(_request("nope"), {})
    assert not outcome.ok
    assert outcome.response["error"]["code"] == -32601
    assert outcome.response["id"] == 1

  def test_requires_params_but_absent(self):
    registry = {"m": MethodDescriptor(requires_params=True)}
    outcome = dispatch_request(_request("m"), registry)
    assert not outcome.ok
    assert outcome.response["error"]["code"] == -32602

  def test_requires_params_present(self):
    registry = {"m": MethodDescriptor(requires_params=True)}
    outcome = dispatch_request(_request("m", params={"_meta": {}}), registry)
    assert outcome.ok

  def test_params_validator_reports_issues(self):
    registry = {"m": MethodDescriptor(params_validator=lambda p: [] if "x" in p else ["missing x"])}
    outcome = dispatch_request(_request("m", params={"y": 1}), registry)
    assert not outcome.ok
    assert outcome.response["error"]["code"] == -32602
    assert "missing x" in outcome.response["error"]["message"]

  def test_params_validator_passes(self):
    registry = {"m": MethodDescriptor(params_validator=lambda p: [] if "x" in p else ["missing x"])}
    outcome = dispatch_request(_request("m", params={"x": 1}), registry)
    assert outcome.ok

  def test_id_echo_preserves_type(self):
    outcome = dispatch_request({"jsonrpc": "2.0", "id": "abc", "method": "nope"}, {})
    assert outcome.response["id"] == "abc"
    assert isinstance(outcome.response["id"], str)
