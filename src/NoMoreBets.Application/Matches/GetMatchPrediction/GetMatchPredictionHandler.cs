using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchPrediction;

public record GetMatchPredictionCommand(int MatchId) : IRequest<Unit>;

public class GetMatchPredictionHandler(
  IMatchPrediction matchPrediction,
  IUnitOfWork unitOfWork,
  ILogger<GetMatchPredictionHandler> logger) : IRequestHandler<GetMatchPredictionCommand, Unit>
{

  public async Task<Unit> Handle(GetMatchPredictionCommand command, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(command.MatchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
    {
      logger.LogWarning("Match with ID {MatchId} was not found. Skipping prediction.", command.MatchId);
      return Unit.Value;
    }

    string homeName = match.HomeClub?.Name ?? "Home";
    string awayName = match.AwayClub?.Name ?? "Away";

    var request = new MatchPredictionPromptRequest(match);

    logger.LogInformation("Starting match prediction for MatchId {MatchId}: {HomeName} vs {AwayName}", command.MatchId, homeName, awayName);

    var resultStr = await matchPrediction.InvokeAsync(request, cancellationToken).ConfigureAwait(false);

    var analysis = new MatchAnalysis
    {
      MatchId = command.MatchId,
      Code = "gpt-5.1",
      Content = resultStr
    };

    await unitOfWork.Matches.AddMatchAnalysisAsync(analysis, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Match prediction completed for MatchId {MatchId}.", command.MatchId);
    return Unit.Value;
  }
}
