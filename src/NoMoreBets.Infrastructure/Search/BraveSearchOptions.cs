namespace NoMoreBets.Infrastructure.Search;

public sealed class BraveSearchOptions
{
  public const string SectionName = "BraveSearch";

  public string ApiKey { get; init; } = string.Empty;
}

