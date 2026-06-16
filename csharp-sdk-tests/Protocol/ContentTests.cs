using System.Text.Json.Nodes;

using Stackific.Mcp;
using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Tests.Protocol;

/// <summary>
/// Content-block discrimination, resource-contents variants, and tool-result shapes
/// (spec §14.4–§14.5, §16.5–§16.7).
/// </summary>
public sealed class ContentTests
{
  [Fact]
  public void Content_blocks_serialize_with_their_type_discriminator()
  {
    IReadOnlyList<ContentBlock> blocks =
    [
      ContentBlocks.Text("hello"),
      ContentBlocks.Image("AAAA", "image/png"),
      ContentBlocks.Audio("BBBB", "audio/wav"),
      ContentBlocks.Resource(ResourceContents.OfText("docs://readme", "# Title", "text/markdown")),
      ContentBlocks.LinkTo("weather://oslo/current", "Oslo weather", "application/json"),
    ];

    var json = McpJson.Serialize(blocks);

    Assert.Contains("\"type\":\"text\"", json);
    Assert.Contains("\"type\":\"image\"", json);
    Assert.Contains("\"type\":\"audio\"", json);
    Assert.Contains("\"type\":\"resource\"", json);
    Assert.Contains("\"type\":\"resource_link\"", json);
  }

  [Fact]
  public void Content_blocks_round_trip_to_their_concrete_subtype()
  {
    var json = McpJson.Serialize<ContentBlock>(ContentBlocks.Text("hi"));
    var block = McpJson.Deserialize<ContentBlock>(json);
    var text = Assert.IsType<TextContent>(block);
    Assert.Equal("hi", text.Text);
  }

  [Fact]
  public void Embedded_resource_round_trips_its_text_variant()
  {
    var embedded = ContentBlocks.Resource(ResourceContents.OfText("docs://x", "body", "text/plain"));
    var json = McpJson.Serialize<ContentBlock>(embedded);
    var back = Assert.IsType<EmbeddedResource>(McpJson.Deserialize<ContentBlock>(json));
    Assert.Equal("body", back.Resource.Text);
    Assert.Null(back.Resource.Blob);
  }

  [Fact]
  public void Resource_contents_variants_are_distinguished_by_payload_field()
  {
    Assert.Contains("\"text\":", McpJson.Serialize(ResourceContents.OfText("u", "t")));
    Assert.DoesNotContain("\"blob\":", McpJson.Serialize(ResourceContents.OfText("u", "t")));
    Assert.Contains("\"blob\":", McpJson.Serialize(ResourceContents.OfBlob("u", "QkJCQg==")));
  }

  [Fact]
  public void Role_audience_uses_lowercase_wire_values()
  {
    var annotations = new Annotations { Audience = [Role.User, Role.Assistant], Priority = 0.3 };
    var json = McpJson.Serialize(annotations);
    Assert.Contains("\"audience\":[\"user\",\"assistant\"]", json);
  }

  [Fact]
  public void Tool_execution_error_uses_is_error_not_a_protocol_error()
  {
    var result = CallToolResult.FromError("Cannot divide by zero.");
    var json = McpJson.Serialize(result);

    Assert.Contains("\"isError\":true", json);
    Assert.Contains("Cannot divide by zero.", json);
  }

  [Fact]
  public void Tool_with_structured_content_serializes_both_fields()
  {
    var result = new CallToolResult
    {
      Content = [ContentBlocks.Text("{\"tempC\":21}")],
      StructuredContent = new JsonObject { ["tempC"] = 21 },
    };
    var json = McpJson.Serialize(result);

    Assert.Contains("\"content\":", json);
    Assert.Contains("\"structuredContent\":{\"tempC\":21}", json);
    Assert.DoesNotContain("\"isError\"", json); // absent ⇒ false
  }

  [Fact]
  public void Tool_input_schema_is_carried_verbatim()
  {
    var tool = new Tool
    {
      Name = "add",
      InputSchema = new JsonObject
      {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["a"] = new JsonObject { ["type"] = "number" } },
        ["required"] = new JsonArray("a"),
      },
      Annotations = new ToolAnnotations { ReadOnlyHint = true },
    };
    var json = McpJson.Serialize(tool);

    Assert.Contains("\"inputSchema\":{\"type\":\"object\"", json);
    Assert.Contains("\"readOnlyHint\":true", json);
  }
}
