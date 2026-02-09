using System.Globalization;
using System.Text.RegularExpressions;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;

/// <summary>Maps <see cref="MatchDetailsDto"/> to <see cref="GoalTeamMatchData"/> for a given team name.</summary>
public static class MatchDetailsToGoalMapper
{
    /// <summary>Maps match details to goal-format per-team stats for the specified team. Returns null if the team name does not match home or away.</summary>
    public static GoalTeamMatchData? MapToGoalTeamMatchData(MatchDetailsDto match, string teamName)
    {
        var trimmed = teamName.Trim();
        bool isHome;
        string opponent;
        double? teamRating;

        if (string.Equals(match.HomeTeam, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            isHome = true;
            opponent = match.AwayTeam;
            teamRating = match.HomeLineup?.TeamRating;
        }
        else if (string.Equals(match.AwayTeam, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            isHome = false;
            opponent = match.HomeTeam;
            teamRating = match.AwayLineup?.TeamRating;
        }
        else
        {
            return null;
        }

        var rows = GetRowsByLabel(match.Statistics);
        string? GetFor(string label) => TryGetRow(rows, label, out var row) ? (isHome ? row.HomeValue : row.AwayValue) : null;
        string? GetAgainst(string label) => TryGetRow(rows, label, out var row) ? (isHome ? row.AwayValue : row.HomeValue) : null;
        string? GetTeamOnly(string label) => GetFor(label);

        var xgFor = TryParseDouble(GetFor("Expected goals (xG)"));
        var xgAgainst = TryParseDouble(GetAgainst("Expected goals (xG)"));
        var shotsFor = TryParseInt(GetFor("Total shots"));
        var shotsAgainst = TryParseInt(GetAgainst("Total shots"));
        var shotsOnTargetFor = TryParseInt(GetFor("Shots on target"));
        var shotsOnTargetAgainst = TryParseInt(GetAgainst("Shots on target"));
        var bigChancesFor = TryParseInt(GetFor("Big chances"));
        var bigChancesAgainst = TryParseInt(GetAgainst("Big chances"));
        var possession = ParsePercentage(GetTeamOnly("Ball possession"));
        var corners = TryParseInt(GetTeamOnly("Corners"));
        var touchesBox = TryParseInt(GetTeamOnly("Touches in opposition box"));
        var passes = TryParseInt(GetTeamOnly("Passes"));
        var passAccuracy = ParsePercentageFromAccuratePasses(GetTeamOnly("Accurate passes"));
        var tackles = TryParseInt(GetTeamOnly("Tackles"));
        var interceptions = TryParseInt(GetTeamOnly("Interceptions"));
        var keeperSaves = TryParseInt(GetTeamOnly("Keeper saves"));
        var distanceKm = ParseDistanceKm(GetTeamOnly("Distance covered"));
        var sprints = TryParseInt(GetTeamOnly("Number of sprints"));

        var date = match.MatchDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new GoalTeamMatchData(
            Team: trimmed,
            Opponent: opponent,
            Date: date,
            IsHome: isHome,
            XgFor: xgFor,
            XgAgainst: xgAgainst,
            ShotsFor: shotsFor,
            ShotsAgainst: shotsAgainst,
            ShotsOnTargetFor: shotsOnTargetFor,
            ShotsOnTargetAgainst: shotsOnTargetAgainst,
            BigChancesFor: bigChancesFor,
            BigChancesAgainst: bigChancesAgainst,
            Possession: possession,
            Corners: corners,
            TouchesBox: touchesBox,
            Passes: passes,
            PassAccuracy: passAccuracy,
            Tackles: tackles,
            Interceptions: interceptions,
            KeeperSaves: keeperSaves,
            DistanceKm: distanceKm,
            Sprints: sprints,
            TeamRating: teamRating);
    }

    private static IReadOnlyDictionary<string, StatRowDto> GetRowsByLabel(IReadOnlyList<StatGroupDto>? statistics)
    {
        var dict = new Dictionary<string, StatRowDto>(StringComparer.Ordinal);
        if (statistics is null) return dict;
        foreach (var group in statistics)
        {
            if (group.Rows is null) continue;
            foreach (var row in group.Rows)
            {
                if (row.Label is { } label && !dict.ContainsKey(label))
                    dict[label] = row;
            }
        }
        return dict;
    }

    private static bool TryGetRow(IReadOnlyDictionary<string, StatRowDto> rows, string label, out StatRowDto row)
    {
        return rows.TryGetValue(label, out row!);
    }

    private static int? TryParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static double? TryParseDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return double.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    /// <summary>Parses "67%" or "90%" to 0.67, 0.9.</summary>
    private static double? ParsePercentage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        var match = Regex.Match(s, @"(\d+(?:\.\d+)?)\s*%");
        if (!match.Success) return null;
        return double.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint | NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n / 100.0
            : null;
    }

    /// <summary>Parses "616 (90%)" to 0.9 by taking the percentage in parentheses.</summary>
    private static double? ParsePercentageFromAccuratePasses(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = Regex.Match(value.Trim(), @"\(\s*(\d+(?:\.\d+)?)\s*%\s*\)");
        if (!match.Success) return ParsePercentage(value);
        return double.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint | NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n / 100.0
            : null;
    }

    /// <summary>Parses "115.8 km" to 115.8.</summary>
    private static double? ParseDistanceKm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        if (s.EndsWith(" km", StringComparison.OrdinalIgnoreCase))
            s = s[..^3].Trim();
        return double.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;
    }
}
