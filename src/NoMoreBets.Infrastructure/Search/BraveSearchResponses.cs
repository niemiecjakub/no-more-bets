using System.Text.Json.Serialization;

namespace NoMoreBets.Infrastructure.Search;

// Web search

public sealed class BraveWebSearchResponse
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("query")]
  public BraveQueryInfo Query { get; set; } = new();

  [JsonPropertyName("web")]
  public BraveWebSection Web { get; set; } = new();
}

public sealed class BraveQueryInfo
{
  [JsonPropertyName("original")]
  public string Original { get; set; } = string.Empty;

  [JsonPropertyName("country")]
  public string Country { get; set; } = string.Empty;
}

public sealed class BraveWebSection
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("results")]
  public List<BraveWebResult> Results { get; set; } = [];
}

public sealed class BraveWebResult
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("title")]
  public string Title { get; set; } = string.Empty;

  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  [JsonPropertyName("meta_url")]
  public BraveMetaUrl? MetaUrl { get; set; }

  [JsonPropertyName("thumbnail")]
  public BraveThumbnail? Thumbnail { get; set; }
}

public sealed class BraveMetaUrl
{
  [JsonPropertyName("scheme")]
  public string Scheme { get; set; } = string.Empty;

  [JsonPropertyName("netloc")]
  public string Netloc { get; set; } = string.Empty;

  [JsonPropertyName("hostname")]
  public string Hostname { get; set; } = string.Empty;

  [JsonPropertyName("favicon")]
  public string Favicon { get; set; } = string.Empty;

  [JsonPropertyName("path")]
  public string Path { get; set; } = string.Empty;
}

public sealed class BraveThumbnail
{
  [JsonPropertyName("src")]
  public string Src { get; set; } = string.Empty;
}

// News search

public sealed class BraveNewsSearchResponse
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("query")]
  public BraveNewsQuery Query { get; set; } = new();

  [JsonPropertyName("results")]
  public List<BraveNewsResult> Results { get; set; } = [];
}

public sealed class BraveNewsQuery
{
  [JsonPropertyName("original")]
  public string Original { get; set; } = string.Empty;

  [JsonPropertyName("country")]
  public string Country { get; set; } = string.Empty;
}

public sealed class BraveNewsResult
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("title")]
  public string Title { get; set; } = string.Empty;

  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  [JsonPropertyName("description")]
  public string Description { get; set; } = string.Empty;

  [JsonPropertyName("age")]
  public string Age { get; set; } = string.Empty;

  [JsonPropertyName("page_age")]
  public string PageAge { get; set; } = string.Empty;

  [JsonPropertyName("meta_url")]
  public BraveMetaUrl? MetaUrl { get; set; }

  [JsonPropertyName("thumbnail")]
  public BraveThumbnail? Thumbnail { get; set; }

  [JsonPropertyName("extra_snippets")]
  public List<string> ExtraSnippets { get; set; } = [];
}

// LLM context

public sealed class BraveLlmContextResponse
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = string.Empty;

  [JsonPropertyName("results")]
  public List<BraveLlmContextItem> Results { get; set; } = [];
}

public sealed class BraveLlmContextItem
{
  [JsonPropertyName("content")]
  public string Content { get; set; } = string.Empty;

  [JsonPropertyName("url")]
  public string? Url { get; set; }

  [JsonPropertyName("tokens")]
  public int Tokens { get; set; }

  [JsonPropertyName("title")]
  public string? Title { get; set; }

  [JsonPropertyName("score")]
  public double? Score { get; set; }

  [JsonPropertyName("source_type")]
  public string? SourceType { get; set; }
}

