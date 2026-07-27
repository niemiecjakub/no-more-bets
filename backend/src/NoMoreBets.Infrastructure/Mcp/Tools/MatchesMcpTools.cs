using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using NoMoreBets.Application.Betting.GetMatchBettingOdds;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Matches.GetMatchEvents;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Infrastructure.Mcp.Tools;

/// <summary>Read-only MCP adapters over the <c>Application.Matches</c> slice.</summary>
[McpServerToolType]
public sealed class MatchesMcpTools(IMediator mediator)
{
  [McpServerTool(
    Name = "matches_search",
    Title = "Search matches",
    ReadOnly = true)]
  [Description("Searches and browses matches. Start here to resolve a team or fixture into a matchId, then use the match-scoped tools. Use search for free-text/semantic lookup, leagueIds and matchStatusId to narrow, and afterMatchDateUtc/afterId as a cursor while hasMore is true.")]
  public Task<Paged<MatchDto>> SearchAsync(
    [Description("Page size (number of matches to return).")] int limit = 20,
    [Description("Optional free-text / semantic search query, e.g. a club name.")] string? search = null,
    [Description("Optional match status filter: 1 = Upcomming, 2 = Finished.")] int? matchStatusId = null,
    [Description("Optional league ids to include; omit or empty for all leagues.")] int[]? leagueIds = null,
    [Description("Cursor: return matches after this UTC match date.")] DateTime? afterMatchDateUtc = null,
    [Description("Cursor: return matches after this id when dates tie.")] int? afterId = null,
    [Description("Sort by match date Ascending or Descending (default Descending).")] MatchDateSortOrder sortOrder = MatchDateSortOrder.Descending,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(
      new GetMatchesPageQuery(
        limit,
        matchStatusId,
        leagueIds ?? [],
        afterMatchDateUtc,
        afterId,
        sortOrder,
        search),
      cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getLineups",
    Title = "Look up lineups",
    ReadOnly = true)]
  [Description("Returns the stored starting lineups for both clubs of a match. Null when no lineup has been collected yet.")]
  public Task<MatchLineupResult?> GetLineupsAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getInjuries",
    Title = "Check injuries",
    ReadOnly = true)]
  [Description("Returns injured or otherwise unavailable players for both clubs of a match. Null when no lineup data exists for the match.")]
  public Task<MatchInjuriesResult?> GetInjuriesAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getHeadToHeadStats",
    Title = "Review head-to-head",
    ReadOnly = true)]
  [Description("Returns aggregated historical head-to-head statistics between the two clubs of a match.")]
  public Task<H2H?> GetHeadToHeadStatsAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getEvents",
    Title = "Read match events",
    ReadOnly = true)]
  [Description("Returns the timeline of recorded events (goals, cards, substitutions) for a match, ordered by minute.")]
  public Task<IReadOnlyList<MatchEventDto>> GetEventsAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetMatchEventsQuery(matchId), cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getCurrentOdds",
    Title = "Check current odds",
    ReadOnly = true)]
  [Description("Returns the latest stored odds for a match. By default returns compact markets (1X2, both teams to score, double chance, over/under goals); set includeExoticMarkets true to also get handicap and exact-score lines. Empty when no odds have been collected.")]
  public Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(
    [Description("The match identifier.")] int matchId,
    [Description("When false (default), omits handicap and exact-score markets.")] bool includeExoticMarkets = false,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetMatchBettingOddsQuery(matchId, includeExoticMarkets), cancellationToken);
  }

  [McpServerTool(
    Name = "matches_getResearch",
    Title = "Read match research",
    ReadOnly = true)]
  [Description("Returns the latest stored research for a match. Null when the match has not been researched yet.")]
  public Task<MatchResearchOutputDto?> GetResearchAsync(
    [Description("The match identifier.")] int matchId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetMatchAgentResearchQuery(matchId), cancellationToken);
  }
}
