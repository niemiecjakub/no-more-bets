using System;
using System.Collections.Generic;

namespace NoMoreBets.Application.Search.SearchBasic;

public sealed class SearchBasicResultDto
{
  public IReadOnlyList<SearchBasicResultItemDto> Items { get; init; } = Array.Empty<SearchBasicResultItemDto>();
}

public sealed class SearchBasicResultItemDto
{
  public string Title { get; init; } = string.Empty;
  public string Url { get; init; } = string.Empty;
  public string Snippet { get; init; } = string.Empty;
  public string? Hostname { get; init; }
  public string? DisplayUrlPath { get; init; }
  public string? ThumbnailUrl { get; init; }
}
