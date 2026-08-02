using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.ResearchBetScenarioStats;

public record MatchResearchBetSlipDto(
  BetSlipSummary Slip,
  ResearchBetScenarioStatsDto? Scenarios);

public record ResearchBetScenarioStatsDto(
  decimal UnitStake,
  ResearchBetParlayScenarioDto Parlay,
  ResearchBetSinglesScenarioDto Singles);

public record ResearchBetParlayScenarioDto(
  decimal StakeTotal,
  decimal CombinedOdds,
  decimal PotentialPayout,
  decimal? Profit);

public record ResearchBetSinglesScenarioDto(
  decimal StakeTotal,
  decimal PotentialPayout,
  decimal? Profit,
  IReadOnlyList<ResearchBetSingleLegDto> Legs);

public record ResearchBetSingleLegDto(
  decimal Stake,
  decimal Odds,
  BetStatus Status,
  decimal? Profit);
