using System.Text.Json;
using System.Text.Json.Nodes;

using Stackific.Mcp.JsonRpc;

namespace Stackific.Mcp.Server;

/// <summary>
/// Pragmatic validation of <c>tools/call</c> arguments against a tool's <c>inputSchema</c> (spec
/// §16.4/§16.6). It enforces the most common JSON Schema 2020-12 constraints — <c>required</c>,
/// top-level property <c>type</c>, and <c>enum</c> — and reports failures as <c>-32602</c> (Invalid
/// params), the canonical code for argument-validation failure (§22.4). It is intentionally not a
/// full schema engine (no <c>$ref</c>/composition/conditionals); a tool needing stricter checks can
/// validate inside its handler.
/// </summary>
internal static class SchemaValidation
{
  public static void ValidateArguments(JsonObject schema, JsonObject arguments, string toolName)
  {
    if (schema["required"] is JsonArray required)
    {
      foreach (var entry in required)
      {
        if (entry is JsonValue value && value.GetValueKind() == JsonValueKind.String)
        {
          var key = value.GetValue<string>();
          if (!arguments.ContainsKey(key))
          {
            throw McpError.InvalidParams(
              $"Missing required argument \"{key}\" for tool \"{toolName}\".",
              new JsonObject { ["toolName"] = toolName, ["argument"] = key });
          }
        }
      }
    }

    if (schema["properties"] is not JsonObject properties) return;

    foreach (var (name, propertySchemaNode) in properties)
    {
      if (propertySchemaNode is not JsonObject propertySchema) continue;
      if (arguments[name] is not JsonNode value) continue; // absent / null → nothing to check here

      if (propertySchema["type"] is JsonValue typeValue && typeValue.GetValueKind() == JsonValueKind.String)
      {
        var type = typeValue.GetValue<string>();
        if (!MatchesType(type, value))
        {
          throw McpError.InvalidParams(
            $"Argument \"{name}\" for tool \"{toolName}\" must be of type {type}.",
            new JsonObject { ["toolName"] = toolName, ["argument"] = name });
        }
      }

      if (propertySchema["enum"] is JsonArray allowed && !allowed.Any(option => JsonNode.DeepEquals(option, value)))
      {
        throw McpError.InvalidParams(
          $"Argument \"{name}\" for tool \"{toolName}\" is not one of the allowed values.",
          new JsonObject { ["toolName"] = toolName, ["argument"] = name });
      }
    }
  }

  private static bool MatchesType(string type, JsonNode value)
  {
    var kind = value.GetValueKind();
    return type switch
    {
      "string" => kind == JsonValueKind.String,
      "number" => kind == JsonValueKind.Number,
      "integer" => kind == JsonValueKind.Number && value is JsonValue n && n.TryGetValue(out long _),
      "boolean" => kind is JsonValueKind.True or JsonValueKind.False,
      "object" => kind == JsonValueKind.Object,
      "array" => kind == JsonValueKind.Array,
      "null" => kind == JsonValueKind.Null,
      _ => true,
    };
  }
}
