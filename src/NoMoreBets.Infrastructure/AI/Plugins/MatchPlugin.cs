using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Leagues.GetLeagueTable;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Application.Matches.GetMatchPreview;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using System.ComponentModel;
using AvailableMatch = NoMoreBets.Infrastructure.AI.Plugins.Models.AvailableMatch;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MatchPlugin
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;
  private readonly ILogger<MatchPlugin> _logger;

  public MatchPlugin(IUnitOfWork unitOfWork, IMediator mediator, ILogger<MatchPlugin>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
    _logger = logger ?? NullLogger<MatchPlugin>.Instance;
  }

  [KernelFunction("GetLineups")]
  [Description("Retrieves the starting lineups for both the home and away teams for the match.")]
  public async Task<MatchLineupResult?> GetLineupsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetInjuries")]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<MatchInjuriesResult?> GetInjuriesAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchPreview")]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchPreviewQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetHead2HeadStats")]
  [Description("Provides historical head-to-head statistics between the two clubs for the match.")]
  public async Task<H2H?> GetHead2HeadStatsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubDailySummary")]
  [Description("Gets the daily summary for a club.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubDailySummaryQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRecentGames")]
  [Description("Retrieves the results from the last 5 league matches for a specific club.")]
  public async Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRecentGamesQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubLeagueStatistics")]
  [Description("Retrieves league table standing and advanced metrics (xG, xGA, xPts) for a club.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubLeagueStatisticsQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetLeagueTable")]
  [Description("Returns the full league table for the league of the match.")]
  public async Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var match = await _unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken).ConfigureAwait(false);

    if (match?.Stage?.Season?.League is not { } league)
    {
      _logger.LogWarning("Cannot load league table because league context is missing for match {MatchId}.", matchId);
      return null;
    }

    return await _mediator.Send(new GetLeagueTableQuery(league.Id), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchBettingOddsHistory")]
  [Description("Provides the movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchBettingOddsHistoryQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetClubRollingPerformance")]
  [Description("Gets performance data for a club from its latest 5 finished games.")]
  public async Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRollingPerformanceQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetUpcomingMatches")]
  [Description("Returns a list with upcomming matches.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default)
  {
    var matches = await _unitOfWork.Matches.GetUpcomingMatchesAsync(cancellationToken).ConfigureAwait(false);

    return matches
      .Select(m => new AvailableMatch(m.Id, m.HomeClub.Name, m.AwayClub.Name, m.MatchDate))
      .ToList();
  }
}
