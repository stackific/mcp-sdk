"""Tests for abstract message-kind predicates (§2.2)."""

from mcp.protocol.messages import (
  is_notification,
  is_request,
  is_valid_abstract_notification,
  is_valid_abstract_request,
  is_valid_error_payload,
)


class TestPredicates:
  def test_is_request(self):
    assert is_request({"id": 1, "method": "m"})
    assert not is_request({"method": "m"})

  def test_is_notification(self):
    assert is_notification({"method": "m"})
    assert not is_notification({"id": 1, "method": "m"})
    assert not is_notification({"id": 1, "result": {}})


class TestAbstractRequest:
  def test_valid_including_null_id(self):
    # The abstract base permits a null id (R-2.2-d); the concrete wire layer is stricter.
    assert is_valid_abstract_request({"id": None, "method": "m"})
    assert is_valid_abstract_request({"id": 1, "method": "m", "params": {}})
    assert is_valid_abstract_request({"id": "x", "method": "m"})

  def test_invalid(self):
    assert not is_valid_abstract_request({"method": "m"})  # no id
    assert not is_valid_abstract_request({"id": 1})  # no method
    assert not is_valid_abstract_request({"id": True, "method": "m"})  # bool id
    assert not is_valid_abstract_request({"id": 1, "method": "m", "params": []})


class TestAbstractNotification:
  def test_valid(self):
    assert is_valid_abstract_notification({"method": "m"})
    assert is_valid_abstract_notification({"method": "m", "params": {}})

  def test_invalid(self):
    assert not is_valid_abstract_notification({"id": 1, "method": "m"})
    assert not is_valid_abstract_notification({"params": {}})


class TestErrorPayload:
  def test_valid(self):
    assert is_valid_error_payload({"code": -32600, "message": "x"})
    assert is_valid_error_payload({"code": -1, "message": "x", "data": {}})

  def test_invalid(self):
    assert not is_valid_error_payload({"code": "x", "message": "y"})
    assert not is_valid_error_payload({"code": True, "message": "y"})
    assert not is_valid_error_payload({"message": "y"})
