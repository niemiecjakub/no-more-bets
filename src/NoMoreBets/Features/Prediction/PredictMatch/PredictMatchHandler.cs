using MediatR;
using NoMoreBets.Features.SoccerData;

namespace NoMoreBets.Features.Prediction.PredictMatch;

/// <summary>
/// Query to run multi-agent prediction for a specific football match.
/// </summary>
public sealed record PredictMatchQuery(
    string HomeTeam,
    string AwayTeam,
    int HomeTeamId,
    int AwayTeamId,
    string BookmakerGameUrl,
    int MatchId,
    int? HomeFotmobTeamId = null,
    int? AwayFotmobTeamId = null,
    int LeagueId = SoccerDataConstants.PremierLeagueId
  ) : IRequest<PredictMatchResult>;

/// <summary>
/// CQRS handler entrypoint for match prediction feature.
/// </summary>
public sealed class PredictMatchHandler(IPredictMatchAgentOrchestrator orchestrator) : IRequestHandler<PredictMatchQuery, PredictMatchResult>
{
    public Task<PredictMatchResult> Handle(PredictMatchQuery request, CancellationToken cancellationToken)
    {
        return orchestrator.RunAsync(request, cancellationToken);
    }
}
