namespace NoMoreBets.Application.Betting.Common;

public record BetSlipListItemDto(
  int Id,
  DateTime CreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  int StatusId,
  string StatusName,
  IReadOnlyList<BetSelectionItemDto> Selections,
  int? AgentSessionId,
  string? Rationale,
  decimal? EstimatedWinProbability,
  int? RiskLevelId,
  string? RiskLevelName,
  DateOnly? SlipDate);
