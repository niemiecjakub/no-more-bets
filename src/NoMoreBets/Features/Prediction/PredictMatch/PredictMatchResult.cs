namespace NoMoreBets.Features.Prediction.PredictMatch;

/// <summary>
/// Result of PredictMatch multi-agent orchestration.
/// </summary>
public sealed record PredictMatchResult
{
  public required string BettingTicket { get; init; }
  public IReadOnlyList<PredictMatchAgentMessage> Transcript { get; init; } = [];
}
public sealed record PredictMatchAgentMessage(string Author, string Content);
