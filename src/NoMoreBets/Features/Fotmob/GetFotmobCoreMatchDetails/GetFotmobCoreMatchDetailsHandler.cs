using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;

namespace NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;

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
