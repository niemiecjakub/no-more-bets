namespace NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;

/// <summary>Per-team match stats in goal format (snake_case JSON).</summary>
public record GoalTeamMatchData(
    string Team,
    string Opponent,
    string? Date,
    bool IsHome,
    double? XgFor,
    double? XgAgainst,
    int? ShotsFor,
    int? ShotsAgainst,
    int? ShotsOnTargetFor,
    int? ShotsOnTargetAgainst,
    int? BigChancesFor,
    int? BigChancesAgainst,
    double? Possession,
    int? Corners,
    int? TouchesBox,
    int? Passes,
    double? PassAccuracy,
    int? Tackles,
    int? Interceptions,
    int? KeeperSaves,
    double? DistanceKm,
    int? Sprints,
    double? TeamRating);
