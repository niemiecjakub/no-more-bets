using System;
using System.Collections.Generic;

namespace NoMoreBets.Application.Search;

public sealed class SearchResultDto
{
  public IReadOnlyList<SearchResultItemDto> Items { get; init; } = Array.Empty<SearchResultItemDto>();
}

public sealed class SearchResultItemDto
{
  public string Title { get; init; } = string.Empty;
  public string Url { get; init; } = string.Empty;
  public string Snippet { get; init; } = string.Empty;
  public string? Hostname { get; init; }
  public string? DisplayUrlPath { get; init; }
  public string? ThumbnailUrl { get; init; }
}

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

public sealed class SearchLlmContextResultDto
{
  public IReadOnlyList<SearchLlmContextItemDto> Items { get; init; } = Array.Empty<SearchLlmContextItemDto>();
}

public sealed class SearchLlmContextItemDto
{
  public string Text { get; init; } = string.Empty;
  public string? Url { get; init; }
  public int? TokenCount { get; init; }
  public string? Title { get; init; }
  public double? Score { get; init; }
  public string? SourceType { get; init; }
}

