using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm.Dtos;

/// <summary>Rolling form averages over the last 5 games (core match stats).</summary>
public record ClubRollingFormDto(
    double? AvgXgFor,
    double? AvgXgAgainst,
    double? AvgShotsOnTargetFor,
    double? AvgShotsOnTargetAgainst,
    double? AvgBigChancesFor,
    double? AvgBigChancesAgainst,
    double? AvgTouchesBox,
    double? AvgPossession,
    IReadOnlyList<GoalTeamMatchData> Details);
