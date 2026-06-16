"""Tests for capability negotiation + gating (§6.1–§6.4)."""

from mcp.protocol.capability_negotiation import (
  CAPABILITY_ERROR_HTTP_STATUS,
  MISSING_CLIENT_CAPABILITY_CODE,
  client_declares,
  client_should_expect_notification,
  compute_missing_client_capabilities,
  decide_degradation,
  gate_required_client_capabilities,
  http_status_for_capability_error,
  is_deprecated_client_capability,
  may_client_invoke,
  may_use_include_context,
  may_use_url_elicitation,
  server_declares,
  server_method_required_capability,
)


class TestClientDeclares:
  def test_object_capabilities(self):
    assert client_declares({"elicitation": {}}, "elicitation")
    assert client_declares({"roots": {}}, "roots")
    assert not client_declares({}, "sampling")

  def test_elicitation_form_implicit_baseline(self):
    assert client_declares({"elicitation": {}}, "elicitation.form")

  def test_elicitation_url_needs_subflag(self):
    assert client_declares({"elicitation": {"url": {}}}, "elicitation.url")
    assert not client_declares({"elicitation": {}}, "elicitation.url")

  def test_sampling_subflags(self):
    assert client_declares({"sampling": {"tools": {}}}, "sampling.tools")
    assert not client_declares({"sampling": {}}, "sampling.context")


class TestServerDeclares:
  def test_object_capabilities(self):
    assert server_declares({"tools": {}}, "tools")
    assert not server_declares({}, "resources")

  def test_boolean_subflags_only_when_true(self):
    assert server_declares({"tools": {"listChanged": True}}, "tools.listChanged")
    assert not server_declares({"tools": {"listChanged": False}}, "tools.listChanged")
    assert not server_declares({"tools": {}}, "tools.listChanged")
    assert server_declares({"resources": {"subscribe": True}}, "resources.subscribe")


class TestMethodGating:
  def test_required_capability(self):
    assert server_method_required_capability("tools/call") == "tools"
    assert server_method_required_capability("server/discover") is None

  def test_may_client_invoke(self):
    assert may_client_invoke("tools/list", {"tools": {}})
    assert not may_client_invoke("tools/list", {})
    assert may_client_invoke("server/discover", {})  # core method always invocable


class TestNotificationGating:
  def test_expect_notification(self):
    assert client_should_expect_notification("notifications/tools/list_changed", {"tools": {"listChanged": True}})
    assert not client_should_expect_notification("notifications/tools/list_changed", {"tools": {}})


class TestMissingCapabilityGate:
  def test_compute_missing(self):
    assert compute_missing_client_capabilities({"sampling": {}}, {"sampling": {}, "roots": {}}) == {"roots": {}}

  def test_gate_ok(self):
    assert gate_required_client_capabilities({"roots": {}}, {"roots": {}}).ok

  def test_gate_missing(self):
    result = gate_required_client_capabilities({}, {"sampling": {}})
    assert not result.ok
    assert result.error["code"] == MISSING_CLIENT_CAPABILITY_CODE
    assert result.error["data"]["requiredCapabilities"] == {"sampling": {}}

  def test_http_status(self):
    assert http_status_for_capability_error(MISSING_CLIENT_CAPABILITY_CODE) == CAPABILITY_ERROR_HTTP_STATUS
    assert http_status_for_capability_error(-32601) is None


class TestSubflagUsage:
  def test_url_elicitation(self):
    assert may_use_url_elicitation({"elicitation": {"url": {}}})
    assert not may_use_url_elicitation({"elicitation": {}})

  def test_include_context(self):
    assert may_use_include_context({}, None)
    assert may_use_include_context({}, "none")
    assert not may_use_include_context({}, "allServers")
    assert may_use_include_context({"sampling": {"context": {}}}, "allServers")


class TestDegradation:
  def test_proceed_fallback_reject(self):
    assert decide_degradation(peer_declares_behavior=True, behavior_mandatory=True) == "proceed"
    assert decide_degradation(peer_declares_behavior=False, behavior_mandatory=False) == "fallback"
    assert decide_degradation(peer_declares_behavior=False, behavior_mandatory=True) == "reject"


class TestDeprecated:
  def test_client(self):
    assert is_deprecated_client_capability("roots")
    assert not is_deprecated_client_capability("elicitation")
