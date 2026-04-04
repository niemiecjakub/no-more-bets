using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchPrediction;

public interface IMatchPrediction
{
  Task<string> InvokeAsync(MatchPredictionPromptRequest request, CancellationToken cancellationToken = default);
}


public record MatchPredictionPromptRequest(Match Match);