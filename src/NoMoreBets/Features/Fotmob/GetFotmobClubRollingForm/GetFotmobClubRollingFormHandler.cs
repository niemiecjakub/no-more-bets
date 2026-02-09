using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm;

/// <summary>
/// Handles <see cref="GetFotmobClubRollingFormQuery"/> by fetching club overview and core match details for the last 5 games, then computing averages.
/// </summary>
public class GetFotmobClubRollingFormHandler(IMediator mediator) : IRequestHandler<GetFotmobClubRollingFormQuery, ClubRollingFormDto>
{
    /// <inheritdoc />
    public async Task<ClubRollingFormDto> Handle(GetFotmobClubRollingFormQuery request, CancellationToken cancellationToken)
    {
        var overview = await mediator.Send(new GetFotmobClubOverviewQuery(request.TeamId), cancellationToken).ConfigureAwait(false);
        var lastFive = overview.RecentGames.TakeLast(5).ToList();

        var coreDetails = new List<GoalTeamMatchData>();
        foreach (var game in lastFive)
        {
            if (string.IsNullOrWhiteSpace(game.GameUrl)) continue;
            var detail = await mediator.Send(new GetFotmobCoreMatchDetailsQuery(game.GameUrl, request.TeamName), cancellationToken).ConfigureAwait(false);
            if (detail is not null)
                coreDetails.Add(detail);
        }

        return new ClubRollingFormDto(
            AvgXgFor: Average(coreDetails, x => x.XgFor),
            AvgXgAgainst: Average(coreDetails, x => x.XgAgainst),
            AvgShotsOnTargetFor: AverageInt(coreDetails, x => x.ShotsOnTargetFor),
            AvgShotsOnTargetAgainst: AverageInt(coreDetails, x => x.ShotsOnTargetAgainst),
            AvgBigChancesFor: AverageInt(coreDetails, x => x.BigChancesFor),
            AvgBigChancesAgainst: AverageInt(coreDetails, x => x.BigChancesAgainst),
            AvgTouchesBox: AverageInt(coreDetails, x => x.TouchesBox),
            AvgPossession: Average(coreDetails, x => x.Possession),
            Details: coreDetails);
    }

    private static double? Average(IReadOnlyList<GoalTeamMatchData> list, Func<GoalTeamMatchData, double?> selector)
    {
        var values = list.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? values.Average() : null;
    }

    private static double? AverageInt(IReadOnlyList<GoalTeamMatchData> list, Func<GoalTeamMatchData, int?> selector)
    {
        var values = list.Select(selector).Where(v => v.HasValue).Select(v => (double)v!.Value).ToList();
        return values.Count > 0 ? values.Average() : null;
    }
}
