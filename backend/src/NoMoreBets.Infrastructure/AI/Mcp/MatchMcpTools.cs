using System.ComponentModel;
using ModelContextProtocol.Server;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;

namespace NoMoreBets.Infrastructure.AI.Mcp;

/// <summary>Read-only MCP adapters over <see cref="MatchTool"/>.</summary>
[McpServerToolType]
public sealed class MatchMcpTools(MatchTool matchTool)
{
  [McpServerTool(
    Name = "match_getAvailableMatchesAsync",
    Title = "Browse upcoming matches",
    ReadOnly = true)]
  [Description("Returns a list of upcoming matches ready for research.")]
  public Task<IReadOnlyList<AvailableMatch>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default) =>
    matchTool.GetUpcomingMatchesAsync(cancellationToken);

  [McpServerTool(
    Name = "match_getLineups",
    Title = "Look up lineups",
    ReadOnly = true)]
  [Description("Retrieves the starting lineups for both the home and away teams for the match.")]
  public Task<MatchLineup?> GetLineupsAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetLineupsAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getInjuries",
    Title = "Check injuries",
    ReadOnly = true)]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public Task<MatchInjuriesResult?> GetInjuriesAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetInjuriesAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getHead2HeadStats",
    Title = "Review head-to-head",
    ReadOnly = true)]
  [Description("Provides historical head-to-head statistics between the two clubs for the match.")]
  public Task<H2H?> GetHead2HeadStatsAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetHead2HeadStatsAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getClubDailySummary",
    Title = "Read club daily summary",
    ReadOnly = true)]
  [Description("Gets the daily summary for a club.")]
  public Task<string?> GetClubDailySummaryAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetClubDailySummaryAsync(clubId, cancellationToken);

  [McpServerTool(
    Name = "match_getClubRecentGames",
    Title = "Review recent form",
    ReadOnly = true)]
  [Description("Retrieves the results from the last 5 league matches for a specific club.")]
  public Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetClubRecentGamesAsync(clubId, cancellationToken);

  [McpServerTool(
    Name = "match_getClubLeagueStatistics",
    Title = "Check league stats",
    ReadOnly = true)]
  [Description("Returns one club's current statistics: table position, points, W/D/L record, goals for/against, and expected metrics.")]
  public Task<ClubLeagueStats?> GetClubLeagueStatisticsAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetClubStatistics(clubId, cancellationToken);

  [McpServerTool(
    Name = "match_getLeagueTable",
    Title = "View league table",
    ReadOnly = true)]
  [Description("Returns the full league table for the league of the match.")]
  public Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetLeagueTableAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getGroupTable",
    Title = "View group table",
    ReadOnly = true)]
  [Description("Returns the group table for the group containing this match's teams.")]
  public Task<IReadOnlyList<LeagueTableStanding>?> GetGroupTableAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetGroupTableAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getMatchBettingOddsHistory",
    Title = "Review odds movement",
    ReadOnly = true)]
  [Description("Provides the movement of betting odds for this match across different event types.")]
  public Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetMatchBettingOddsHistoryAsync(matchId, cancellationToken);

  [McpServerTool(
    Name = "match_getClubRollingPerformance",
    Title = "Review recent performance",
    ReadOnly = true)]
  [Description("Gets performance data for a club from its latest 5 finished games.")]
  public Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetClubRollingPerformanceAsync(clubId, cancellationToken);

  [McpServerTool(
    Name = "match_getMatchResearchTextAsync",
    Title = "Read saved research",
    ReadOnly = true)]
  [Description("Returns the latest stored research analysis text for the match.")]
  public Task<string?> GetMatchResearchTextAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default) =>
    matchTool.GetMatchResearchTextAsync(matchId, cancellationToken);
}
