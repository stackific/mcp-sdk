"""Sampling (DEPRECATED) (§21.2).

⚠️ DEPRECATED capability — defined only for interoperability; new model-calling
functionality SHOULD integrate directly with a model provider. Sampling lets a server
obtain a model completion by delegating the call to the client (human-in-the-loop),
delivered via the §11 multi-round-trip ``sampling/createMessage`` input request.

This port owns the §21.2 data shapes + behavioural rules: the tool_use/tool_result
content, the sampling message/content union, model preferences + hint matching, tool
choice + includeContext, the request params + result, the capability gating, and the
§21.2.7 ordering/exclusivity + §21.2.10 consent obligations.
"""

from __future__ import annotations

from dataclasses import dataclass

from mcp.protocol.capability_negotiation import (
  is_deprecated_client_capability,
  may_invoke_sampling,
  may_use_include_context,
  may_use_sampling_tools,
)
from mcp.protocol.errors import INVALID_PARAMS_CODE
from mcp.types.content import is_valid_audio_content, is_valid_image_content, is_valid_text_content

SAMPLING_DEPRECATED = True
SAMPLING_METHOD = "sampling/createMessage"
SAMPLING_INPUT_REQUEST_METHOD = SAMPLING_METHOD
SAMPLING_REPLACEMENT_GUIDANCE = (
  "Sampling is Deprecated. For new model-calling functionality, integrate directly with a "
  "model provider instead of delegating through sampling/createMessage."
)


def is_sampling_deprecated() -> bool:
  """Return ``True`` — the ``sampling`` capability is Deprecated. (R-21.2-a/§21.2.1)"""
  return is_deprecated_client_capability("sampling")


# ─── content blocks (§21.2.6) ─────────────────────────────────────────────────

def _is_number(value: object) -> bool:
  return isinstance(value, (int, float)) and not isinstance(value, bool)


def is_tool_use_content(block: object) -> bool:
  """Return ``True`` for a ``tool_use`` block: ``id``, ``name`` (str) + ``input`` (object).
  (§21.2.6)
  """
  if not isinstance(block, dict) or block.get("type") != "tool_use":
    return False
  return isinstance(block.get("id"), str) and isinstance(block.get("name"), str) and isinstance(block.get("input"), dict)


def is_tool_result_content(block: object) -> bool:
  """Return ``True`` for a ``tool_result`` block: ``toolUseId`` (str) + ``content`` (list).
  (§21.2.6)
  """
  if not isinstance(block, dict) or block.get("type") != "tool_result":
    return False
  if not isinstance(block.get("toolUseId"), str) or not isinstance(block.get("content"), list):
    return False
  return "isError" not in block or isinstance(block["isError"], bool)


def tool_result_is_error(block: dict) -> bool:
  """Return the ``tool_result`` block's ``isError``, defaulting to ``False``. (R-21.2.6-g)"""
  return block.get("isError", False) is True


def is_valid_sampling_content_block(block: object) -> bool:
  """Return ``True`` for a sampling content block: text/image/audio or tool_use/tool_result
  (resource_link/embedded resource are excluded from sampling). (§21.2.6, R-21.2.6-d)
  """
  return (
    is_tool_use_content(block)
    or is_tool_result_content(block)
    or is_valid_text_content(block)
    or is_valid_image_content(block)
    or is_valid_audio_content(block)
  )


def is_valid_sampling_content(content: object) -> bool:
  """Return ``True`` for sampling content: a single block or an array of blocks. (§21.2.6)"""
  if isinstance(content, list):
    return all(is_valid_sampling_content_block(b) for b in content)
  return is_valid_sampling_content_block(content)


def as_content_array(content: object) -> list:
  """Normalise single-or-array sampling content to a list, for uniform iteration."""
  return list(content) if isinstance(content, list) else [content]


def is_valid_sampling_message(value: object) -> bool:
  """Return ``True`` for a ``SamplingMessage``: ``role`` (user/assistant) + ``content``
  (single block or array); OPTIONAL ``_meta``. (§21.2.6)
  """
  if not isinstance(value, dict) or value.get("role") not in ("user", "assistant"):
    return False
  if not is_valid_sampling_content(value.get("content")):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


# ─── ModelHint / ModelPreferences (§21.2.9) ───────────────────────────────────

def is_valid_model_preferences(value: object) -> bool:
  """Return ``True`` for ``ModelPreferences``: OPTIONAL ``hints`` (list of {name?}) and the
  three OPTIONAL 0–1 priorities. (§21.2.9)
  """
  if not isinstance(value, dict):
    return False
  if "hints" in value:
    hints = value["hints"]
    if not isinstance(hints, list):
      return False
    for h in hints:
      if not isinstance(h, dict) or ("name" in h and not isinstance(h["name"], str)):
        return False
  for key in ("costPriority", "speedPriority", "intelligencePriority"):
    if key in value and not (_is_number(value[key]) and 0 <= value[key] <= 1):
      return False
  return True


def select_first_hint_match(hints: list | None, available_models: list[str]) -> dict | None:
  """Select the first hint whose ``name`` substring matches a candidate model (order-sensitive,
  first match). Returns ``{"hint", "model"}`` or ``None``. (R-21.2.9-b/-f)
  """
  if not hints:
    return None
  for hint in hints:
    needle = hint.get("name")
    if needle is None:
      continue
    model = next((m for m in available_models if needle in m), None)
    if model is not None:
      return {"hint": hint, "model": model}
  return None


# ─── ToolChoice (§21.2.5) / includeContext (§21.2.4) ──────────────────────────

TOOL_CHOICE_MODES = ("auto", "required", "none")
DEFAULT_TOOL_CHOICE = {"mode": "auto"}


def resolve_tool_choice(tool_choice: dict | None) -> dict:
  """Resolve the effective ``ToolChoice``, defaulting to ``{"mode": "auto"}``. (R-21.2.4-p)"""
  if tool_choice is not None and tool_choice.get("mode") is not None:
    return {"mode": tool_choice["mode"]}
  return dict(DEFAULT_TOOL_CHOICE)


INCLUDE_CONTEXT_VALUES = ("none", "thisServer", "allServers")
DEPRECATED_INCLUDE_CONTEXT_VALUES = frozenset({"thisServer", "allServers"})


def is_deprecated_include_context(value: str) -> bool:
  """Return ``True`` for a Deprecated ``includeContext`` value. (§21.2.4)"""
  return value in DEPRECATED_INCLUDE_CONTEXT_VALUES


# ─── CreateMessageRequestParams (§21.2.4) ─────────────────────────────────────

def is_valid_sampling_tool(value: object) -> bool:
  """Return ``True`` for a request-scoped sampling ``Tool``: ``name`` (str) + optional
  ``description``/``inputSchema``. (§21.2.4, R-21.2.4-m)
  """
  if not isinstance(value, dict) or not isinstance(value.get("name"), str):
    return False
  if "description" in value and not isinstance(value["description"], str):
    return False
  return "inputSchema" not in value or isinstance(value["inputSchema"], dict)


def is_valid_create_message_request_params(value: object) -> bool:
  """Return ``True`` for ``sampling/createMessage`` params: REQUIRED ``messages`` array +
  numeric ``maxTokens``; OPTIONAL advisory fields. (§21.2.4)
  """
  if not isinstance(value, dict):
    return False
  messages = value.get("messages")
  if not isinstance(messages, list) or not all(is_valid_sampling_message(m) for m in messages):
    return False
  if not _is_number(value.get("maxTokens")):
    return False
  if "includeContext" in value and value["includeContext"] not in INCLUDE_CONTEXT_VALUES:
    return False
  if "modelPreferences" in value and not is_valid_model_preferences(value["modelPreferences"]):
    return False
  if "tools" in value:
    tools = value["tools"]
    if not isinstance(tools, list) or not all(is_valid_sampling_tool(t) for t in tools):
      return False
  return True


def resolve_include_context(params: dict) -> str:
  """Return the effective ``includeContext``, defaulting to ``"none"``. (§21.2.4)"""
  return params.get("includeContext") or "none"


def is_tool_enabled_request(params: dict) -> bool:
  """Return ``True`` when the request carries ``tools`` or ``toolChoice`` (needs
  ``sampling.tools``). (R-21.2.3-a/-b)
  """
  return params.get("tools") is not None or params.get("toolChoice") is not None


def clamp_to_max_tokens(produced: int, max_tokens: int) -> int:
  """Clamp a produced token count to ``maxTokens`` (a hard upper bound). (R-21.2.4-i/-j)"""
  return max_tokens if produced > max_tokens else produced


# ─── CreateMessageResult (§21.2.8) ────────────────────────────────────────────

STANDARD_STOP_REASONS = ("endTurn", "stopSequence", "maxTokens", "toolUse")


def is_standard_stop_reason(reason: str) -> bool:
  """Return ``True`` for one of the four standard ``stopReason`` values (the field is open).
  (§21.2.8)
  """
  return reason in STANDARD_STOP_REASONS


def is_valid_sampling_create_message_result(value: object) -> bool:
  """Return ``True`` for a ``CreateMessageResult``: ``role`` + ``content`` + string ``model`` +
  ``resultType``; OPTIONAL open-string ``stopReason``, ``_meta``. (§21.2.8)
  """
  if not isinstance(value, dict) or value.get("role") not in ("user", "assistant"):
    return False
  if not is_valid_sampling_content(value.get("content")):
    return False
  if not isinstance(value.get("model"), str) or not isinstance(value.get("resultType"), str):
    return False
  if "stopReason" in value and not isinstance(value["stopReason"], str):
    return False
  return "_meta" not in value or isinstance(value["_meta"], dict)


# ─── capability gating (§21.2.3) ──────────────────────────────────────────────

def build_sampling_tools_not_declared_error(field: str) -> dict:
  """Build the -32602 error a client returns when a sampling request includes ``tools``/
  ``toolChoice`` without ``sampling.tools``. (R-21.2.3-b)
  """
  suffix = "n" if field == "tools" else "o"
  return {
    "code": INVALID_PARAMS_CODE,
    "message": f"Sampling request includes `{field}` but the client did not declare `sampling.tools` (R-21.2.3-b, R-21.2.4-{suffix})",
  }


@dataclass(frozen=True)
class SamplingGateResult:
  """Outcome of :func:`gate_sampling_tool_use`."""

  ok: bool
  error: dict | None = None


def gate_sampling_tool_use(client_caps: dict, params: dict) -> SamplingGateResult:
  """Client-side gate: a tool-enabled request without ``sampling.tools`` is rejected
  (``tools`` checked before ``toolChoice``). (R-21.2.3-b)
  """
  if not is_tool_enabled_request(params):
    return SamplingGateResult(True)
  if may_use_sampling_tools(client_caps):
    return SamplingGateResult(True)
  field = "tools" if params.get("tools") is not None else "toolChoice"
  return SamplingGateResult(False, error=build_sampling_tools_not_declared_error(field))


def may_server_send_sampling_request(client_caps: dict, params: dict) -> bool:
  """Server-side gate: a server MAY send only when ``sampling`` is declared, a tool-enabled
  request has ``sampling.tools``, and the ``includeContext`` value is permitted. (R-21.2.3)
  """
  if not may_invoke_sampling(client_caps):
    return False
  if is_tool_enabled_request(params) and not may_use_sampling_tools(client_caps):
    return False
  return may_use_include_context(client_caps, params.get("includeContext"))


@dataclass(frozen=True)
class SamplingRequestValidation:
  """Outcome of :func:`validate_sampling_request`."""

  ok: bool
  params: dict | None = None
  error: dict | None = None


def validate_sampling_request(client_caps: dict, raw_params: object) -> SamplingRequestValidation:
  """Full client-side validation: structural parse (REQUIRED messages + maxTokens) plus the
  tool-use capability gate. (R-21.2.4-a/-h, R-21.2.3-b)
  """
  if not is_valid_create_message_request_params(raw_params):
    return SamplingRequestValidation(
      False, error={"code": INVALID_PARAMS_CODE, "message": "Malformed sampling/createMessage params (messages + maxTokens required)"}
    )
  gate = gate_sampling_tool_use(client_caps, raw_params)
  if not gate.ok:
    return SamplingRequestValidation(False, error=gate.error)
  return SamplingRequestValidation(True, params=raw_params)


# ─── message-content constraints (§21.2.7) ────────────────────────────────────

def validate_user_tool_result_exclusivity(message: dict) -> dict:
  """Validate §21.2.7-a: a ``user`` message with any ``tool_result`` block MUST contain ONLY
  ``tool_result`` blocks. Returns ``{"ok", "reason"?}``.
  """
  if message.get("role") != "user":
    return {"ok": True}
  blocks = as_content_array(message.get("content"))
  if not any(isinstance(b, dict) and b.get("type") == "tool_result" for b in blocks):
    return {"ok": True}
  if all(isinstance(b, dict) and b.get("type") == "tool_result" for b in blocks):
    return {"ok": True}
  return {"ok": False, "reason": "A user message containing tool_result blocks MUST contain ONLY tool_result blocks (R-21.2.7-a)"}


def validate_sampling_message_ordering(messages: list) -> dict:
  """Validate §21.2.7-b: every assistant message with ``tool_use`` blocks MUST be followed
  immediately by a user message of only matching ``tool_result`` blocks (also enforces the
  per-user exclusivity rule). Returns ``{"ok", "reason"?, "index"?}``.
  """
  for i, message in enumerate(messages):
    exclusivity = validate_user_tool_result_exclusivity(message)
    if not exclusivity["ok"]:
      return {"ok": False, "reason": exclusivity["reason"], "index": i}
    if message.get("role") != "assistant":
      continue
    blocks = as_content_array(message.get("content"))
    use_ids = [b["id"] for b in blocks if isinstance(b, dict) and b.get("type") == "tool_use" and isinstance(b.get("id"), str)]
    if not use_ids:
      continue
    nxt = messages[i + 1] if i + 1 < len(messages) else None
    if nxt is None:
      return {"ok": False, "reason": "An assistant message with tool_use MUST be followed immediately by a user tool_result message (R-21.2.7-b)", "index": i}
    if nxt.get("role") != "user":
      return {"ok": False, "reason": "The message after an assistant tool_use MUST be a user message of tool_result blocks (R-21.2.7-b)", "index": i + 1}
    next_blocks = as_content_array(nxt.get("content"))
    if not next_blocks or not all(isinstance(b, dict) and b.get("type") == "tool_result" for b in next_blocks):
      return {"ok": False, "reason": "The user message following an assistant tool_use MUST consist entirely of tool_result blocks (R-21.2.7-b)", "index": i + 1}
    result_ids = {b.get("toolUseId") for b in next_blocks if isinstance(b, dict)}
    for use_id in use_ids:
      if use_id not in result_ids:
        return {"ok": False, "reason": f'tool_use id "{use_id}" has no matching tool_result toolUseId (R-21.2.7-b, R-21.2.6-d)', "index": i + 1}
  return {"ok": True}


def validate_tool_result_references(messages: list) -> dict:
  """Validate §21.2.6-d: every ``tool_result.toolUseId`` refers to an EARLIER ``tool_use.id``.
  Returns ``{"ok", "reason"?, "tool_use_id"?}``.
  """
  seen: set[str] = set()
  for message in messages:
    for block in as_content_array(message.get("content")):
      if not isinstance(block, dict):
        continue
      if block.get("type") == "tool_use":
        if isinstance(block.get("id"), str):
          seen.add(block["id"])
      elif block.get("type") == "tool_result":
        tool_use_id = block.get("toolUseId")
        if not isinstance(tool_use_id, str) or tool_use_id not in seen:
          return {
            "ok": False,
            "reason": "ToolResultContent.toolUseId MUST match the id of a previous ToolUseContent (R-21.2.6-d)",
            "tool_use_id": tool_use_id if isinstance(tool_use_id, str) else None,
          }
  return {"ok": True}


def preserve_content_meta(block: dict) -> dict:
  """Preserve a tool_use/tool_result block's ``_meta`` when carrying it into a later request
  (a shallow copy); other blocks are returned unchanged. (R-21.2.6-c/-h)
  """
  if block.get("type") not in ("tool_use", "tool_result"):
    return block
  return dict(block)


# ─── consent & safety obligations (§21.2.10) ──────────────────────────────────

CLIENT_MODIFIABLE_REQUEST_FIELDS = ("systemPrompt", "includeContext", "temperature", "stopSequences", "metadata")


def is_client_modifiable_request_field(field: str) -> bool:
  """Return ``True`` when ``field`` is one the client MAY modify/omit. (R-21.2.10-e)"""
  return field in CLIENT_MODIFIABLE_REQUEST_FIELDS


@dataclass
class SamplingConsentObligations:
  """The §21.2.10 consent & safety obligations a host claims to meet."""

  human_in_the_loop: bool = False
  user_may_deny: bool = False
  review_prompt_before_sampling: bool = False
  review_result_before_server: bool = False
  may_modify_control_fields: bool = False
  rate_limiting: bool = False
  validate_content: bool = False
  handle_sensitive_data: bool = False
  tool_loop_iteration_limits: bool = False


#: The MUST-level obligations. (R-21.2.10-a/-b/-h)
REQUIRED_CONSENT_OBLIGATIONS = ("human_in_the_loop", "user_may_deny", "handle_sensitive_data")


def unmet_required_consent_obligations(obligations: SamplingConsentObligations) -> list[str]:
  """Return the unmet MUST-level §21.2.10 obligations (empty = satisfied). (R-21.2.10-a/-b/-h)"""
  return [key for key in REQUIRED_CONSENT_OBLIGATIONS if getattr(obligations, key) is not True]


def within_tool_loop_limit(iteration: int, limit: int) -> bool:
  """Return ``True`` when another tool-loop iteration is permitted. (R-21.2.10-i)"""
  return iteration < limit
