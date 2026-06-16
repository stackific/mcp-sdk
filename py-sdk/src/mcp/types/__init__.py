"""MCP type descriptors (§14 and friends).

Currently provides :mod:`mcp.types.implementation` — the ``Implementation`` identity
descriptor used by the per-request ``_meta`` envelope (clientInfo) and by discovery
(serverInfo). Further type modules (content blocks, resources, tools, …) land in later
phases.
"""

from mcp.types.implementation import (
  Implementation,
  is_valid_implementation,
  parse_implementation,
)

__all__ = ["Implementation", "is_valid_implementation", "parse_implementation"]
