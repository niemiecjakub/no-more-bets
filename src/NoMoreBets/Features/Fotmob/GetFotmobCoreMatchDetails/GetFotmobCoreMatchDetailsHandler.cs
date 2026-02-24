using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;

namespace NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;

/// <summary>
/// Query to fetch core match details (goal-format per-team stats) from a FotMob match for a given team.
/// </summary>
/// <param name="GameUrl">FotMob match page URL.</param>
/// <param name="TeamName">Team name (e.g. "Paris Saint-Germain") to get stats for.</param>
public record GetFotmobCoreMatchDetailsQuery(string GameUrl, string TeamName) : IRequest<GoalTeamMatchData?>;

/// <summary>
/// Handles <see cref="GetFotmobCoreMatchDetailsQuery"/> by fetching match details then mapping to goal-format per-team stats.
/// </summary>
public class GetFotmobCoreMatchDetailsHandler(IMediator mediator) : IRequestHandler<GetFotmobCoreMatchDetailsQuery, GoalTeamMatchData?>
{
    /// <inheritdoc />
    public async Task<GoalTeamMatchData?> Handle(GetFotmobCoreMatchDetailsQuery request, CancellationToken cancellationToken)
    {
        var matchDetails = await mediator.Send(new GetFotmobMatchDetailsQuery(request.GameUrl), cancellationToken).ConfigureAwait(false);
        return MatchDetailsToGoalMapper.MapToGoalTeamMatchData(matchDetails, request.TeamName);
    }
}
