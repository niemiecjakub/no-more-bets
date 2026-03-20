using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Application.Matches.GetMatchPreview;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MatchPlugin
{
  private readonly Match _match;
  private readonly IMediator _mediator;

  public MatchPlugin(Match match, IMediator mediator)
  {
    _match = match;
    _mediator = mediator;
  }

  [KernelFunction("GetLineups")]
  [Description("Retrieves the starting lineups for both the home and away teams for the current match.")]
  public async Task<MatchLineupResult?> GetLineupsAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchLineupsQuery(_match.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetInjuries")]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<MatchInjuriesResult?> GetInjuriesAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchInjuriesQuery(_match.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchPreview")]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchPreviewQuery(_match.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetHead2HeadStats")]
  [Description("Provides historical head-to-head statistics between the two clubs.")]
  public async Task<H2H?> GetHead2HeadStatsAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetHeadToHeadStatsQuery(_match.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubDailySummary")]
  [Description("Gets the daily summary for a club as of the current match date (or latest on/before that date).")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var matchDate = DateOnly.FromDateTime(_match.MatchDate);
    return await _mediator.Send(new GetClubDailySummaryQuery(clubId, matchDate), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRecentGames")]
  [Description("Retrieves the last 5 match results for a specific club, including scores and opponents.")]
  public async Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var matchDate = DateOnly.FromDateTime(_match.MatchDate);
    return await _mediator.Send(new GetClubRecentGamesQuery(clubId, matchDate), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubLeagueStatistics")]
  [Description("Retrieves league table standing and advanced metrics (xG, xGA, xPts) for a club as of the current match date.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    var matchDate = DateOnly.FromDateTime(_match.MatchDate);
    return await _mediator.Send(new GetClubLeagueStatisticsQuery(clubId, matchDate), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchBettingOddsHistory")]
  [Description("Provides the historical movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchBettingOddsHistoryQuery(_match.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRollingPerformance")]
  [Description("Gets performance data for a club from its latest 5 finished games on or before the current match date: top players, team ratings, and formations.")]
  public async Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var matchDate = DateOnly.FromDateTime(_match.MatchDate);
    return await _mediator.Send(new GetClubRollingPerformanceQuery(clubId, matchDate), cancellationToken).ConfigureAwait(false);
  }
}
