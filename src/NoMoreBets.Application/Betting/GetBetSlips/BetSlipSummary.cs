using System.ComponentModel;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetBetSlips;

[Description("Bet slip with selections and settlement status")]
public record BetSlipSummary(
  int Id,
  DateTime CreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  BetStatus Status,
  IReadOnlyList<BetSelectionSummary> Selections);

[Description("Single selection on a bet slip")]
public record BetSelectionSummary(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string EventTypeName,
  string OutcomeKey,
  decimal OddsAtPlacement,
  BetStatus Status);
