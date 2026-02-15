namespace NoMoreBets.Features.Prediction.PredictMatch;

/// <summary>
/// OpenAI settings used by Semantic Kernel agents.
/// </summary>
public sealed class OpenAiAgentOptions
{
  public string ModelId { get; init; } = null!;
  public string ApiKey { get; init; } = null!;
}