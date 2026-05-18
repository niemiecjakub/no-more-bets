namespace NoMoreBets.Controllers.Models;

public record ClubDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);

public record BetSelectionItemDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string EventTypeName,
  string EventOptionName,
  decimal OddsAtPlacement,
  int StatusId,
  string StatusName);

public record BetSlipListItemDto(
  int Id,
  DateTime CreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  int StatusId,
  string StatusName,
  IReadOnlyList<BetSelectionItemDto> Selections,
  int? AgentSessionId);

/// <param name="MessageCount">Transcript messages only; excludes function-call (tool) rows.</param>
public record AgentSessionListItemDto(
  int Id,
  int PhaseId,
  string PhaseName,
  DateTime StartedAt,
  int MessageCount,
  int? MatchId = null);

public record AgentSessionMessageDto(int Id, int SessionId, int Ordinal, int Kind, string Text);
