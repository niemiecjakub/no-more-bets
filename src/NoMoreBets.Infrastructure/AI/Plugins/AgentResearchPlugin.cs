using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentResearchPlugin
{
  private readonly MatchPlugin _matchPlugin;
  private readonly SearchPlugin _searchPlugin;
  private readonly MemoriesPlugin _memoriesPlugin;
  private readonly IUnitOfWork _unitOfWork;

  public AgentResearchPlugin(
    MatchPlugin matchPlugin,
    SearchPlugin searchPlugin,
    MemoriesPlugin memoriesPlugin,
    IUnitOfWork unitOfWork)
  {
    _matchPlugin = matchPlugin;
    _searchPlugin = searchPlugin;
    _memoriesPlugin = memoriesPlugin;
    _unitOfWork = unitOfWork;
  }

  [KernelFunction]
  [Description("Retrieves the starting lineups for both the home and away teams for the match.")]
  public Task<NoMoreBets.Application.Matches.GetMatchLineups.MatchLineupResult?> GetLineupsAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetLineupsAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public Task<NoMoreBets.Application.Matches.GetMatchInjuries.MatchInjuriesResult?> GetInjuriesAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetInjuriesAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Retrieves a textual preview of the match.")]
  public Task<string?> GetMatchPreviewAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetMatchPreviewAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Provides historical head-to-head statistics between the two clubs for the match.")]
  public Task<NoMoreBets.Application.Matches.GetHeadToHeadStats.H2H?> GetHead2HeadStatsAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetHead2HeadStatsAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Gets the daily summary for a club.")]
  public Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetClubDailySummaryAsync(clubId, cancellationToken);

  [KernelFunction]
  [Description("Retrieves the last 5 match results for a specific club.")]
  public Task<IReadOnlyList<NoMoreBets.Application.Clubs.GetClubRecentGames.RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetClubRecentGamesAsync(clubId, cancellationToken);

  [KernelFunction]
  [Description("Retrieves league table standing and advanced metrics (xG, xGA, xPts) for a club.")]
  public Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetClubStatistics(clubId, cancellationToken);

  [KernelFunction]
  [Description("Returns the full league table for the league of the match.")]
  public Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetLeagueTableAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Provides the movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public Task<IReadOnlyList<NoMoreBets.Application.Betting.GetMatchBettingOddsHistory.MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(int matchId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetMatchBettingOddsHistoryAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Gets performance data for a club from its latest 5 finished games.")]
  public Task<NoMoreBets.Application.Clubs.GetClubRollingPerformance.TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default) =>
    _matchPlugin.GetClubRollingPerformanceAsync(clubId, cancellationToken);

  [KernelFunction]
  [Description("Returns a list with upcomming matches.")]
  public Task<IReadOnlyList<AvailableMatch>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default) =>
    _matchPlugin.GetUpcomingMatchesAsync(cancellationToken);

  [KernelFunction]
  [Description("Search for recent news articles and current events.")]
  public Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(string query, CancellationToken cancellationToken = default) =>
    _searchPlugin.SearchNewsAsync(query, cancellationToken);

  [KernelFunction]
  [Description("Retrieves high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context for a complex question, or summarizing a specific topic.")]
  public Task<IReadOnlyList<SearchLlmContextItemDto>> GetWebGroundingAsync(string query, CancellationToken cancellationToken = default) =>
    _searchPlugin.GetWebGroundingAsync(query, cancellationToken);

  [KernelFunction]
  [Description("Lists all saved memory records.")]
  public Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default) =>
    _memoriesPlugin.GetMemoryRecordsAsync(cancellationToken);

  [KernelFunction]
  [Description("Loads the full content of a saved memory record.")]
  public Task<string> ReadMemoryAsync(string name, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.ReadAsync(name, cancellationToken);

  [KernelFunction]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer AppendMemoryAsync or ReplaceMemoryAsync for small changes so you do not drop existing text.")]
  public Task<string> WriteMemoryAsync(string name, string text, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.WriteAsync(name, text, cancellationToken);

  [KernelFunction]
  [Description("Adds text to the end of an existing memory record")]
  public Task<string> AppendMemoryAsync(string name, string text, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.AppendAsync(name, text, cancellationToken);

  [KernelFunction]
  [Description("Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public Task<string> ReplaceMemoryAsync(
    string name,
    string oldText,
    string? newText,
    bool replaceAll = false,
    CancellationToken cancellationToken = default) =>
    _memoriesPlugin.ReplaceAsync(name, oldText, newText, replaceAll, cancellationToken);

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
