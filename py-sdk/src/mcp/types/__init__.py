"""MCP type descriptors (§14).

The shared type system the feature methods build on: identity (``Implementation``,
``BaseMetadata``), ``Role``, ``Annotations``, ``ResourceContents``, and the
``ContentBlock`` union. (``Icon`` lands in a later cluster.)
"""

from mcp.types.annotations import is_valid_annotations
from mcp.types.base_metadata import is_valid_base_metadata, resolve_display_name
from mcp.types.content import (
  FORBIDDEN_CONTENT_BLOCK_TYPES,
  KNOWN_CONTENT_BLOCK_TYPES,
  is_forbidden_content_block_type,
  is_known_content_block_type,
  is_valid_audio_content,
  is_valid_content_block,
  is_valid_embedded_resource,
  is_valid_image_content,
  is_valid_resource_link,
  is_valid_text_content,
)
from mcp.types.implementation import (
  Implementation,
  is_valid_implementation,
  parse_implementation,
)
from mcp.types.resource_contents import (
  is_valid_base64,
  is_valid_blob_resource_contents,
  is_valid_resource_contents,
  is_valid_text_resource_contents,
)
from mcp.types.role import ROLES, is_role

__all__ = [
  "Implementation",
  "is_valid_implementation",
  "parse_implementation",
  "is_valid_base_metadata",
  "resolve_display_name",
  "ROLES",
  "is_role",
  "is_valid_annotations",
  "is_valid_base64",
  "is_valid_text_resource_contents",
  "is_valid_blob_resource_contents",
  "is_valid_resource_contents",
  "KNOWN_CONTENT_BLOCK_TYPES",
  "FORBIDDEN_CONTENT_BLOCK_TYPES",
  "is_known_content_block_type",
  "is_forbidden_content_block_type",
  "is_valid_text_content",
  "is_valid_image_content",
  "is_valid_audio_content",
  "is_valid_resource_link",
  "is_valid_embedded_resource",
  "is_valid_content_block",
]
