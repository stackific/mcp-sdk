using System.Text.Json.Serialization;

namespace Stackific.Mcp.Protocol;

/// <summary>
/// The reference identifying what a <c>completion/complete</c> request is completing (spec §19.3):
/// either a prompt (<c>ref/prompt</c>) or a resource template (<c>ref/resource</c>). The closed
/// union is discriminated by the <c>type</c> field.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PromptReference), "ref/prompt")]
[JsonDerivedType(typeof(ResourceTemplateReference), "ref/resource")]
public abstract record CompletionReference
{
  private protected CompletionReference() { }
}

/// <summary>A completion reference identifying a prompt by name (spec §19.3, <c>type: "ref/prompt"</c>).</summary>
public sealed record PromptReference : CompletionReference
{
  /// <summary>REQUIRED. The programmatic name of the prompt being completed.</summary>
  public required string Name { get; init; }

  /// <summary>OPTIONAL. A human display name (not load-bearing for matching).</summary>
  public string? Title { get; init; }
}

/// <summary>A completion reference identifying a resource template by URI (spec §19.3, <c>type: "ref/resource"</c>).</summary>
public sealed record ResourceTemplateReference : CompletionReference
{
  /// <summary>REQUIRED. The URI or URI template whose variable is being completed.</summary>
  public required string Uri { get; init; }
}

/// <summary>The single argument being completed in a <c>completion/complete</c> request (spec §19.2).</summary>
public sealed record CompletionArgument
{
  /// <summary>REQUIRED. The name of the argument being completed.</summary>
  public required string Name { get; init; }

  /// <summary>REQUIRED. The current partial value (the match seed); MAY be empty.</summary>
  public required string Value { get; init; }
}

/// <summary>Additional disambiguating context for completion (spec §19.2).</summary>
public sealed record CompletionContext
{
  /// <summary>OPTIONAL. Already-resolved sibling arguments, keyed by name (excluding the completed argument).</summary>
  public IDictionary<string, string>? Arguments { get; init; }
}

/// <summary>The parameters of a <c>completion/complete</c> request (spec §19.2).</summary>
public sealed record CompleteRequestParams
{
  /// <summary>REQUIRED. What is being completed (a prompt or resource-template reference).</summary>
  public required CompletionReference Ref { get; init; }

  /// <summary>REQUIRED. The argument being completed.</summary>
  public required CompletionArgument Argument { get; init; }

  /// <summary>OPTIONAL. Additional context to refine suggestions.</summary>
  public CompletionContext? Context { get; init; }
}

/// <summary>The ranked candidate values returned by a completion (spec §19.4).</summary>
public sealed record CompletionValues
{
  /// <summary>REQUIRED. Candidate values, ranked by descending relevance; MUST NOT exceed 100 items.</summary>
  public required IReadOnlyList<string> Values { get; init; }

  /// <summary>OPTIONAL. The total number of matches available (MAY exceed <see cref="Values"/>).</summary>
  public int? Total { get; init; }

  /// <summary>OPTIONAL (default <c>false</c>). Whether more matches exist beyond <see cref="Values"/>.</summary>
  public bool? HasMore { get; init; }
}

/// <summary>The result of a <c>completion/complete</c> request (spec §19.4).</summary>
public sealed record CompleteResult
{
  /// <summary>REQUIRED. The completion suggestions.</summary>
  public required CompletionValues Completion { get; init; }
}
