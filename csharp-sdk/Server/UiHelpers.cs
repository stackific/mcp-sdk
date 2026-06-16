using System.Text.Json.Nodes;

using Stackific.Mcp.Protocol;

namespace Stackific.Mcp.Server;

/// <summary>
/// Helpers for the Interactive User-Interface extension (spec §26): a server returns an interactive UI
/// by embedding a <c>ui://</c> resource (<c>text/html;profile=mcp-app</c>) in a tool result, which the
/// host renders sandboxed. The C# counterpart of the SDK's <c>uiToolResult</c>.
/// </summary>
public static class UiHelpers
{
  /// <summary>
  /// Builds a tool result that launches an MCP App (spec §26.3/§26.4): a text fallback block plus an
  /// embedded <c>ui://</c> resource carrying the app HTML, with the tool's <c>_meta.ui</c> declaring the
  /// resource URI.
  /// </summary>
  /// <param name="resourceUri">The <c>ui://</c> URI identifying the app resource.</param>
  /// <param name="html">The app HTML (served as <c>text/html;profile=mcp-app</c>).</param>
  /// <param name="text">An optional text fallback for hosts that do not render UI.</param>
  /// <returns>The tool result.</returns>
  public static CallToolResult UiToolResult(string resourceUri, string html, string? text = null)
  {
    var content = new List<ContentBlock>();
    if (text is not null) content.Add(ContentBlocks.Text(text));
    content.Add(ContentBlocks.Resource(ResourceContents.OfText(resourceUri, html, UiResource.MimeType)));
    return new CallToolResult
    {
      Content = content,
      Meta = new JsonObject { ["ui"] = new JsonObject { ["resourceUri"] = resourceUri } },
    };
  }
}
