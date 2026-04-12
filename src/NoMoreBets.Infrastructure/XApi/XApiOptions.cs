namespace NoMoreBets.Infrastructure.XApi;

public sealed class XApiOptions
{
  public const string SectionName = "XApi";

  public string BearerToken { get; init; } = string.Empty;
}
