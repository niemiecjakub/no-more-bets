namespace NoMoreBets.Infrastructure.AI;

public sealed class OpenAIOptions
{
  public const string SectionName = "OpenAI";

  public string ModelId { get; init; } = string.Empty;

  public string ApiKey { get; init; } = string.Empty;
}
