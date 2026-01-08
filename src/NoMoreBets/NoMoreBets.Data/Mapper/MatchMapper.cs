using System.Globalization;
using NoMoreBets.Data.Models;

namespace NoMoreBets.Data.Mapper;

public static class MatchMapper
{
  public static IEnumerable<Match> ToMatches(this IEnumerable<MatchRaw> rawMatches)
  {
    return rawMatches.Select(ToMatch);
  }

  public static Match ToMatch(this MatchRaw raw)
  {
    var match = new Match
    {
      Division = raw.Division,
      Date = ParseDate(raw.DateString),
      Time = ParseTime(raw.Time),
      Referee = raw.Referee,
      FullTimeResult = ParseMatchResult(raw.FullTimeResultString),
      HalfTimeResult = ParseMatchResult(raw.HalfTimeResultString)
    };

    match.Teams[TeamSide.Home] = CreateTeamData(
        raw.HomeTeam,
        raw.FullTimeHomeGoals,
        raw.HalfTimeHomeGoals,
        raw.HomeShots,
        raw.HomeShotsOnTarget,
        raw.HomeCorners,
        raw.HomeFouls,
        raw.HomeOffsides,
        raw.HomeYellowCards,
        raw.HomeRedCards
    );

    match.Teams[TeamSide.Away] = CreateTeamData(
        raw.AwayTeam,
        raw.FullTimeAwayGoals,
        raw.HalfTimeAwayGoals,
        raw.AwayShots,
        raw.AwayShotsOnTarget,
        raw.AwayCorners,
        raw.AwayFouls,
        raw.AwayOffsides,
        raw.AwayYellowCards,
        raw.AwayRedCards
    );

    return match;
  }

  private static DateTime ParseDate(string? dateString)
  {
    if (string.IsNullOrWhiteSpace(dateString))
    {
      return DateTime.MinValue;
    }

    // Try dd/MM/yyyy format first
    if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
    {
      return date;
    }

    // Try dd/MM/yy format as fallback
    if (DateTime.TryParseExact(dateString, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateShort))
    {
      return dateShort;
    }

    return DateTime.MinValue;
  }

  private static TimeSpan ParseTime(string? timeString)
  {
    if (string.IsNullOrWhiteSpace(timeString))
    {
      return TimeSpan.Zero;
    }

    // Try HH:mm format (24-hour format as used in CSV)
    if (TimeSpan.TryParseExact(timeString, "HH\\:mm", CultureInfo.InvariantCulture, out var timeExact))
    {
      return timeExact;
    }

    // Fallback to standard parsing
    if (TimeSpan.TryParse(timeString, out var time))
    {
      return time;
    }

    return TimeSpan.Zero;
  }

  private static MatchResult? ParseMatchResult(string? resultString)
  {
    if (string.IsNullOrWhiteSpace(resultString) || resultString.Length == 0)
    {
      return null;
    }

    return resultString[0] switch
    {
      'H' => MatchResult.Home,
      'D' => MatchResult.Draw,
      'A' => MatchResult.Away,
      _ => null
    };
  }

  private static TeamMatchData CreateTeamData(
      string teamName,
      int? fullTimeGoals,
      int? halfTimeGoals,
      int? shots,
      int? shotsOnTarget,
      int? corners,
      int? fouls,
      int? offsides,
      int? yellowCards,
      int? redCards)
  {
    return new TeamMatchData
    {
      TeamName = teamName,
      FullTimeGoals = fullTimeGoals,
      HalfTimeGoals = halfTimeGoals,
      Shots = shots,
      ShotsOnTarget = shotsOnTarget,
      Corners = corners,
      Fouls = fouls,
      Offsides = offsides,
      YellowCards = yellowCards,
      RedCards = redCards
    };
  }
}

