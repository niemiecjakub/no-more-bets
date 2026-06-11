using System.ComponentModel;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Leagues.GetLeagueTable;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using AvailableMatch = NoMoreBets.Infrastructure.AI.Tools.Implementations.Models.AvailableMatch;
using ToolTeamLineup = NoMoreBets.Infrastructure.AI.Tools.Implementations.Models.TeamLineup;
using ToolPlayer = NoMoreBets.Infrastructure.AI.Tools.Implementations.Models.Player;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations;

public class MatchTool
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly ILogger<MatchTool> _logger;

  public MatchTool(
    IUnitOfWork unitOfWork,
    IMediator mediator,
    AgentSessionContext agentSessionContext,
    ILogger<MatchTool>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
    _agentSessionContext = agentSessionContext;
    _logger = logger ?? NullLogger<MatchTool>.Instance;
  }

  [Description("Retrieves the starting lineups for both the home and away teams for the match.")]
  public async Task<MatchLineup?> GetLineupsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var result = await _mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken).ConfigureAwait(false);
    if (result is null)
    {
      return null;
    }

    return new MatchLineup(
      new ToolTeamLineup(result.Home.Players.Select(p => new ToolPlayer(p.Name, p.Position)).ToList()),
      new ToolTeamLineup(result.Away.Players.Select(p => new ToolPlayer(p.Name, p.Position)).ToList()));
  }

  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<MatchInjuriesResult?> GetInjuriesAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Provides historical head-to-head statistics between the two clubs for the match.")]
  public async Task<H2H?> GetHead2HeadStatsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Gets the daily summary for a club.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubDailySummaryQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Retrieves the results from the last 5 league matches for a specific club.")]
  public async Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRecentGamesQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Retrieves league table standing and advanced metrics (xG, xGA, xPts) for a club.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubLeagueStatisticsQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

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

  [Description("Provides the movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetMatchBettingOddsHistoryQuery(matchId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Gets performance data for a club from its latest 5 finished games.")]
  public async Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetClubRollingPerformanceQuery(clubId), cancellationToken).ConfigureAwait(false);
  }

  [Description("Returns a list with upcomming matches.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default)
  {
    var matches = await _unitOfWork.Matches.GetUpcomingMatchesAsync(cancellationToken).ConfigureAwait(false);

    return matches
      .Select(m => new AvailableMatch(m.Id, m.HomeClub.Name, m.AwayClub.Name, m.MatchDate))
      .ToList();
  }

  [Description("Stores research text.")]
  public async Task<string> SaveMatchAnalysisAsync(
    [Description("The match identifier.")]
    int matchId,
    [Description("Research content to store.")]
    string content,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(content))
    {
      _logger.LogError("Saving empty research content for match {MatchId}.", matchId);
    }

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

  [Description("Returns the latest stored research analysis text for the match (same source used before betting). Use to compare pre-match thesis to how the bet resolved.")]
  public async Task<string?> GetMatchResearchTextAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var research = await _mediator
      .Send(new GetMatchAgentResearchQuery(matchId), cancellationToken)
      .ConfigureAwait(false);

    if (research is null)
    {
      _logger.LogError("No reflection research text found for match {MatchId}.", matchId);
      return "Match analysis is not available.";
    }

    return MatchAnalysis.FormatResearchOutput(new MatchResearchOutput
    {
      MatchOverview = research.MatchOverview,
      KeyPoints = research.KeyPoints.ToList(),
      RisksAndUnknowns = research.RisksAndUnknowns.ToList(),
    });
  }

  private static string SerializeResearchText(string content)
  {
    var payload = new ResearchText(content);
    _ = payload.Text;
    return JsonSerializer.Serialize(payload);
  }
}
