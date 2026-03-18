using System;
using System.Collections.Generic;

namespace NoMoreBets.Application.Search.SearchLlmContext;

public sealed class SearchLlmContextResultDto
{
  public IReadOnlyList<SearchLlmContextItemDto> Items { get; init; } = Array.Empty<SearchLlmContextItemDto>();
}

public sealed class SearchLlmContextItemDto
{
  public string? Url { get; init; }
  public string? Title { get; init; }
  public IReadOnlyList<string> Snippets { get; init; } = Array.Empty<string>();
  public string? Hostname { get; init; }
  public string? Age { get; init; }
}
