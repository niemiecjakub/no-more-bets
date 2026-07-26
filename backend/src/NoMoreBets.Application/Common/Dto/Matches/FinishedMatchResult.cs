namespace NoMoreBets.Application.Common.Dto.Matches;

public sealed record FinishedMatchResult(
  string ExternalId,
  string HomeTeam,
  string AwayTeam,
  DateOnly MatchDate,
  TimeOnly? KickoffTime,
  int HomeGoals,
  int AwayGoals,
  string? DetailUrl = null);
