using System;
using System.Collections.Generic;

namespace NoMoreBets.Application.Search.SearchNews;

public sealed class SearchNewsResultDto
{
  public IReadOnlyList<SearchNewsArticleDto> Items { get; init; } = Array.Empty<SearchNewsArticleDto>();
}

public sealed class SearchNewsArticleDto
{
  public string Title { get; init; } = string.Empty;
  public string Url { get; init; } = string.Empty;
  public string Source { get; init; } = string.Empty;
  public DateTimeOffset? PublishedAt { get; init; }
  public string Snippet { get; init; } = string.Empty;
  public string? Age { get; init; }
  public DateTimeOffset? PageAge { get; init; }
  public string? Hostname { get; init; }
  public string? ThumbnailUrl { get; init; }
  public IReadOnlyList<string> ExtraSnippets { get; init; } = Array.Empty<string>();
}
