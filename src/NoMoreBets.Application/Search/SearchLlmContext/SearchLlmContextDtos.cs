using System;
using System.Collections.Generic;

namespace NoMoreBets.Application.Search.SearchLlmContext;

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
