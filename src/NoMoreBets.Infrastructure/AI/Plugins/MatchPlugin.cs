using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MatchPlugin
{
  private static readonly JsonSerializerOptions JsonOptionsNoCycles = new(JsonSerializerDefaults.Web)
  {
    WriteIndented = false,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
  };

  private readonly int _matchId;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<MatchPlugin> _logger;

  public MatchPlugin(
    int matchId,
    IUnitOfWork unitOfWork,
    ILogger<MatchPlugin> logger)
  {
    _matchId = matchId;
    _unitOfWork = unitOfWork;
    _logger = logger;
  }

  [KernelFunction("GetLineups")]
  [Description("Retrieves the starting lineups for both the home and away teams for the current match.")]
  public async Task<MatchLineupResult?> GetLineupsAsync(CancellationToken cancellationToken = default)
  {
    var lineup = await _unitOfWork.Matches.GetLineup(_matchId).ConfigureAwait(false);
    if (lineup == null)
      return null;
    var homeLineup = lineup.GetHomeTeamLineup();
    var awayLineup = lineup.GetAwayTeamLineup();

    return new MatchLineupResult(
      Home: new TeamLineupResult(homeLineup.LineupType, homeLineup.Players),
      Away: new TeamLineupResult(awayLineup.LineupType, awayLineup.Players));
  }

  [KernelFunction("GetInjuries")]
  [Description("Gets a list of injured or unavailable players for both teams involved in the match.")]
  public async Task<MatchInjuriesResult?> GetInjuriesAsync(CancellationToken cancellationToken = default)
  {
    var lineup = await _unitOfWork.Matches.GetLineup(_matchId).ConfigureAwait(false);
    if (lineup == null)
      return null;
    var homeLineup = lineup.GetHomeTeamLineup();
    var awayLineup = lineup.GetAwayTeamLineup();

    return new MatchInjuriesResult(
      Home: new TeamInjuriesResult(homeLineup.Injuries),
      Away: new TeamInjuriesResult(awayLineup.Injuries));
  }

  [KernelFunction("GetMatchPreview")]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(CancellationToken cancellationToken = default)
  {
    var preview = await _unitOfWork.Matches.GetMatchPreview(_matchId).ConfigureAwait(false);
    if (preview == null || string.IsNullOrWhiteSpace(preview.PreviewContentJson))
      return null;

    return preview.PreviewContentJson;
  }

  [KernelFunction("GetHead2HeadStats")]
  [Description("Provides historical head-to-head statistics between the two clubs.")]
  public async Task<string?> GetHead2HeadStatsAsync(CancellationToken cancellationToken = default)
  {
    var match = await _unitOfWork.Matches.GetMatchByIdAsync(_matchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return null;

    var head2head = await _unitOfWork.Matches.GetHeadToHead(match.HomeClubId, match.AwayClubId).ConfigureAwait(false);
    if (head2head == null || string.IsNullOrWhiteSpace(head2head.Head2HeadJson))
      return null;

    return head2head.Head2HeadJson;
  }

  [KernelFunction("GetClubDailySummary")]
  [Description("Gets the most recent daily summary for a specific club based on its ID.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var summary = await _unitOfWork.Clubs.GetLatestDailySummaryAsync(clubId, cancellationToken).ConfigureAwait(false);
    if (summary == null)
      return null;

    return $"[{summary.Date}] {summary.Summary}";
  }

  [KernelFunction("GetClubRecentGames")]
  [Description("Retrieves the last 5 match results for a specific club, including scores and opponents.")]
  public async Task<IReadOnlyList<RecentMatch>?> GetClubRecentGamesAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken).ConfigureAwait(false);
    if (club == null)
      return null;

    var matches = await _unitOfWork.Matches.GetRecentMatchesForClubAsync(clubId, 5, cancellationToken).ConfigureAwait(false);
    if (matches.Count == 0)
      return Array.Empty<RecentMatch>();

    var recentMatches = new List<RecentMatch>(matches.Count);
    foreach (var m in matches)
    {
      var isHome = m.HomeClubId == clubId;
      var opponentName = isHome ? m.AwayClub.Name : m.HomeClub.Name;
      var homeGoals = m.HomeGoals ?? 0;
      var awayGoals = m.AwayGoals ?? 0;
      var score = $"{homeGoals} : {awayGoals}";
      var result = isHome
        ? (homeGoals > awayGoals ? "Win" : homeGoals < awayGoals ? "Loss" : "Draw")
        : (awayGoals > homeGoals ? "Win" : awayGoals < homeGoals ? "Loss" : "Draw");
      var recentMatch = new RecentMatch(MatchId: m.Id, Opponent: opponentName, Score: score, Result: result, Date: m.MatchDate);
      recentMatches.Add(recentMatch);
    }
    return recentMatches.OrderByDescending(g => g.Date).ToList();
  }

  [KernelFunction("GetMatchBettingOddsHistory")]
  [Description("Provides the historical movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<MatchBettingOddsHistoryResult?> GetMatchBettingOddsHistoryAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var snapshots = await _unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken)
      .ConfigureAwait(false);

    if (snapshots.Count == 0)
      return null;

    var byEventType = new Dictionary<int, EventTypeOddsAccumulator>();

    foreach (var snapshot in snapshots)
    {
      foreach (var row in snapshot.Rows)
      {
        if (!byEventType.TryGetValue(row.EventTypeId, out var acc))
        {
          acc = new EventTypeOddsAccumulator
          {
            EventTypeName = row.EventTypeEntity?.Name ?? $"EventType{row.EventTypeId}"
          };
          byEventType[row.EventTypeId] = acc;
        }

        BookmakerEvent? ev = null;
        try
        {
          ev = JsonSerializer.Deserialize<BookmakerEvent>(row.EventJson, JsonOptionsNoCycles);
        }
        catch
        {
          continue;
        }

        if (ev == null) continue;

        if (acc.Title == null)
          acc.Title = ev.Title;
        if (acc.OptionOrder.Count == 0)
          acc.OptionOrder.AddRange(ev.Options.Select(o => o.Label));

        foreach (var opt in ev.Options)
        {
          if (!acc.OddsByLabel.TryGetValue(opt.Label, out var list))
          {
            list = new List<(double Odds, DateTime At)>();
            acc.OddsByLabel[opt.Label] = list;
          }
          list.Add((opt.Odds, snapshot.SnapshotTime));
        }
      }
    }

    var sections = byEventType.Select(kv =>
    {
      var acc = kv.Value;
      var options = acc.OptionOrder.Select(label =>
      {
        var segments = CollapseToSegments(
          acc.OddsByLabel.TryGetValue(label, out var o) ? o : (IReadOnlyList<(double Odds, DateTime At)>)Array.Empty<(double, DateTime)>());
        return new OddsHistoryOptionResult(label, segments);
      }).ToList();
      return new BettingOddsHistorySectionResult(kv.Key, acc.EventTypeName, acc.Title, options);
    }).ToList();

    return new MatchBettingOddsHistoryResult(matchId, sections);
  }

  private static IReadOnlyList<OddsSegmentResult> CollapseToSegments(IReadOnlyList<(double Odds, DateTime At)> points)
  {
    if (points.Count == 0)
      return Array.Empty<OddsSegmentResult>();

    var sorted = points.OrderBy(p => p.At).ToList();
    var segments = new List<OddsSegmentResult>();
    var startTime = sorted[0].At;
    var currentOdds = sorted[0].Odds;

    for (var i = 1; i < sorted.Count; i++)
    {
      if (sorted[i].Odds != currentOdds)
      {
        segments.Add(new OddsSegmentResult(currentOdds, startTime, sorted[i].At));
        currentOdds = sorted[i].Odds;
        startTime = sorted[i].At;
      }
    }
    segments.Add(new OddsSegmentResult(currentOdds, startTime, EndTime: null));
    return segments;
  }
}
