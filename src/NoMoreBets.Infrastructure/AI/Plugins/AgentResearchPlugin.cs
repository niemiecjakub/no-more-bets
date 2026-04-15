using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentResearchPlugin : AgentPluginBase
{
  private readonly MatchPlugin _matchPlugin;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IAgentSessionContext _agentSessionContext;

  public AgentResearchPlugin(
    MatchPlugin matchPlugin,
    SearchPlugin searchPlugin,
    MemoriesPlugin memoriesPlugin,
    IUnitOfWork unitOfWork,
    IAgentSessionContext agentSessionContext)
    : base(memoriesPlugin, searchPlugin)
  {
    _matchPlugin = matchPlugin;
    _unitOfWork = unitOfWork;
    _agentSessionContext = agentSessionContext;
  }

  [KernelFunction]
  [Description("Retrieves the starting lineups for both the home and away teams for the match.")]
  public async Task<NoMoreBets.Application.Matches.GetMatchLineups.MatchLineupResult?> GetLineupsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetLineupsAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<NoMoreBets.Application.Matches.GetMatchInjuries.MatchInjuriesResult?> GetInjuriesAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetInjuriesAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetMatchPreviewAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Provides historical head-to-head statistics between the two clubs for the match.")]
  public async Task<NoMoreBets.Application.Matches.GetHeadToHeadStats.H2H?> GetHead2HeadStatsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetHead2HeadStatsAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Gets the daily summary for a club.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetClubDailySummaryAsync(clubId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Retrieves the last 5 match results for a specific club.")]
  public async Task<IReadOnlyList<NoMoreBets.Application.Clubs.GetClubRecentGames.RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetClubRecentGamesAsync(clubId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Retrieves league table standing and advanced metrics (xG, xGA, xPts) for a club.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetClubStatistics(clubId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the full league table for the league of the match.")]
  public async Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetLeagueTableAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Provides the movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<NoMoreBets.Application.Betting.GetMatchBettingOddsHistory.MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetMatchBettingOddsHistoryAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Gets performance data for a club from its latest 5 finished games.")]
  public async Task<NoMoreBets.Application.Clubs.GetClubRollingPerformance.TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetClubRollingPerformanceAsync(clubId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns a list with upcomming matches.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetUpcomingMatchesAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Stores research text.")]
  public async Task<string> SaveMatchAnalysisAsync(
    [Description("The match identifier.")]
    int matchId,
    [Description("Research content to store.")]
    string content,
    CancellationToken cancellationToken = default)
  {
    var normalizedContent = SerializeResearchText(content);

    var analysis = new MatchAnalysis
    {
      MatchId = matchId,
      AgentSessionId = _agentSessionContext.SessionId,
      Code = MatchAnalysis.ResearchCode,
      Content = normalizedContent
    };

    await _unitOfWork.Matches.AddMatchAnalysisAsync(analysis, cancellationToken).ConfigureAwait(false);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Match research saved";
  }

  private static string SerializeResearchText(string content)
  {
    var payload = new ResearchText(content);
    _ = payload.Text;
    return JsonSerializer.Serialize(payload);
  }
}
