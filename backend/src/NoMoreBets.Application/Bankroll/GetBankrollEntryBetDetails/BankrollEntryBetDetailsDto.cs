using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Bankroll.GetBankrollEntryBetDetails;

public record BankrollEntryBetDetailsDto(
  int EntryId,
  int BetId,
  DateTime BetCreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  int StatusId,
  string StatusName,
  int? AgentSessionId,
  IReadOnlyList<BetSelectionItemDto> Selections,
  string? Rationale,
  decimal? EstimatedWinProbability);
