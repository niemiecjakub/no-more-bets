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
using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MatchPlugin
{
  private readonly int _matchId;
  private readonly IMediator _mediator;

  public MatchPlugin(int matchId, IMediator mediator)
  {
    _matchId = matchId;
    _mediator = mediator;
  }

  [KernelFunction("GetLineups")]
  [Description("Retrieves the starting lineups for both the home and away teams for the current match.")]
  public async Task<MatchLineupResult?> GetLineupsAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchLineupsQuery(_matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetInjuries")]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<MatchInjuriesResult?> GetInjuriesAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchInjuriesQuery(_matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchPreview")]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchPreviewQuery(_matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetHead2HeadStats")]
  [Description("Provides historical head-to-head statistics between the two clubs.")]
  public async Task<H2H?> GetHead2HeadStatsAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetHeadToHeadStatsQuery(_matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubDailySummary")]
  [Description("Gets the most recent daily summary for a specific club based on its ID.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubDailySummaryQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRecentGames")]
  [Description("Retrieves the last 5 match results for a specific club, including scores and opponents.")]
  public async Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRecentGamesQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubLeagueStatistics")]
  [Description("Retrieves the current league table standing and advanced performance metrics (xG, xGA, xPts) for a specific club.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubLeagueStatisticsQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchBettingOddsHistory")]
  [Description("Provides the historical movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchBettingOddsHistoryQuery(_matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRollingPerformance")]
  [Description("Gets performance data for a club from its latest 5 games: top players with recent ratings and average rating, team performances (recent team ratings and average), and formations used in each match.")]
  public async Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRollingPerformanceQuery(clubId), cancellationToken).ConfigureAwait(false);
  }
}
