using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using System.ComponentModel;
using System.Text.Json;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MatchPlugin
{
  private static readonly HashSet<BettingEventType> BettingOddsHistoryEventTypeWhitelist = new()
  {
    BettingEventType.OverUnderGoals,
    BettingEventType.TeamGoals,
    BettingEventType.DoubleChance,
    BettingEventType.BothTeamsToScore,
    BettingEventType.MatchResult,
    BettingEventType.Handicap,
    BettingEventType.ExactScore,
  };

  private readonly int _matchId;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<MatchPlugin> _logger;
  private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
  public MatchPlugin(int matchId, IUnitOfWork unitOfWork, ILogger<MatchPlugin> logger)
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
      Home: new TeamLineupResult(homeLineup.LineupType.ToString(), homeLineup.Players.Select(p => new Player(p.Player, p.Position.ToString())).ToList()),
      Away: new TeamLineupResult(awayLineup.LineupType.ToString(), awayLineup.Players.Select(p => new Player(p.Player, p.Position.ToString())).ToList()));
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
      Home: new TeamInjuriesResult(homeLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()),
      Away: new TeamInjuriesResult(awayLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()));
  }

  [KernelFunction("GetMatchPreview")]
  [Description("Retrieves a textual preview of the match.")]
  public async Task<string?> GetMatchPreviewAsync(CancellationToken cancellationToken = default)
  {
    var preview = await _unitOfWork.Matches.GetMatchPreview(_matchId).ConfigureAwait(false);
    return preview?.BuildMarkdownPreview() ?? "No preview available.";
  }

  [KernelFunction("GetHead2HeadStats")]
  [Description("Provides historical head-to-head statistics between the two clubs.")]
  public async Task<H2H?> GetHead2HeadStatsAsync(CancellationToken cancellationToken = default)
  {
    var match = await _unitOfWork.Matches.GetMatchByIdAsync(_matchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return null;

    var head2head = await _unitOfWork.Matches.GetHeadToHead(match.HomeClubId, match.AwayClubId).ConfigureAwait(false);
    if (head2head == null || string.IsNullOrWhiteSpace(head2head.Head2HeadJson))
      return null;

    var dto = head2head.GetHeadToHead();
    if (dto == null || dto.Stats?.Overall == null)
      return null;

    return MapToLlmFriendly(dto, match, head2head);
  }

  private static H2H MapToLlmFriendly(HeadToHead dto, Match match, Head2Head entity)
  {
    var overall = dto.Stats.Overall;
    var totalMatches = overall.OverallGamesPlayed;
    var homeIsTeam1 = match.HomeClubId == entity.Team1Id;

    TeamMetrics teamA;
    TeamMetrics teamB;
    if (homeIsTeam1)
    {
      teamA = BuildTeamMetrics(
        match.HomeClub.Name,
        overall.OverallTeam1Wins,
        overall.OverallTeam1Scored,
        overall.OverallTeam2Scored,
        dto.Stats.Team1AtHome.Team1WinsAtHome,
        dto.Stats.Team2AtHome.Team2LossesAtHome,
        totalMatches);
      teamB = BuildTeamMetrics(
        match.AwayClub.Name,
        overall.OverallTeam2Wins,
        overall.OverallTeam2Scored,
        overall.OverallTeam1Scored,
        dto.Stats.Team2AtHome.Team2WinsAtHome,
        dto.Stats.Team1AtHome.Team1LossesAtHome,
        totalMatches);
    }
    else
    {
      teamA = BuildTeamMetrics(
        match.HomeClub.Name,
        overall.OverallTeam2Wins,
        overall.OverallTeam2Scored,
        overall.OverallTeam1Scored,
        dto.Stats.Team2AtHome.Team2WinsAtHome,
        dto.Stats.Team1AtHome.Team1LossesAtHome,
        totalMatches);
      teamB = BuildTeamMetrics(
        match.AwayClub.Name,
        overall.OverallTeam1Wins,
        overall.OverallTeam1Scored,
        overall.OverallTeam2Scored,
        dto.Stats.Team1AtHome.Team1WinsAtHome,
        dto.Stats.Team2AtHome.Team2LossesAtHome,
        totalMatches);
    }

    return new H2H
    {
      Summary = $"{match.HomeClub.Name} vs {match.AwayClub.Name}",
      TotalMatches = totalMatches,
      TotalDraws = overall.OverallDraws,
      TeamA = teamA,
      TeamB = teamB
    };
  }

  private static TeamMetrics BuildTeamMetrics(
    string name,
    int totalWins,
    int totalGoalsScored,
    int totalGoalsConceded,
    int homeWins,
    int awayWins,
    int totalMatches)
  {
    return new TeamMetrics
    {
      Name = name,
      TotalWins = totalWins,
      TotalGoalsScored = totalGoalsScored,
      TotalGoalsConceded = totalGoalsConceded,
      HomeWins = homeWins,
      AwayWins = awayWins,
      WinPercentage = totalMatches > 0 ? totalWins * 100.0 / totalMatches : 0,
      AvgGoalsScored = totalMatches > 0 ? (double)totalGoalsScored / totalMatches : 0,
      AvgGoalsConceded = totalMatches > 0 ? (double)totalGoalsConceded / totalMatches : 0
    };
  }

  [KernelFunction("GetClubDailySummary")]
  [Description("Gets the most recent daily summary for a specific club based on its ID.")]
  public async Task<string?> GetClubDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var summary = await _unitOfWork.Clubs.GetLatestDailySummaryAsync(clubId, cancellationToken).ConfigureAwait(false);
    return summary?.ToString() ?? "No daily summary available.";
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
      var recentMatch = new RecentMatch(MatchId: m.Id, Opponent: opponentName, Score: score, Result: result, Date: DateOnly.FromDateTime(m.MatchDate));
      recentMatches.Add(recentMatch);
    }
    return recentMatches.OrderByDescending(g => g.Date).ToList();
  }

  [KernelFunction("GetClubLeagueStatistics")]
  [Description("Retrieves the current league table standing and advanced performance metrics (xG, xGA, xPts) for a specific club.")]
  public async Task<ClubLeagueStats?> GetClubStatistics(int clubId, CancellationToken cancellationToken = default)
  {
    return await _unitOfWork.Clubs.GetCurrentClubLeagueStatsAsync(clubId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetMatchBettingOddsHistory")]
  [Description("Provides the historical movement of betting odds for this match, showing how prices have changed over time across different event types.")]
  public async Task<IReadOnlyList<MarketPriceHistory>?> GetMatchBettingOddsHistoryAsync(CancellationToken cancellationToken = default)
  {
    var snapshots = await _unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(_matchId, cancellationToken).ConfigureAwait(false);

    if (snapshots.Count == 0)
    {
      return null;
    }

    var byEventType = new Dictionary<int, EventTypeOddsAccumulator>();

    foreach (var snapshot in snapshots)
    {
      foreach (var row in snapshot.Rows)
      {
        var eventType = (BettingEventType)row.EventTypeId;
        if (!BettingOddsHistoryEventTypeWhitelist.Contains(eventType))
          continue;

        if (!byEventType.TryGetValue(row.EventTypeId, out var acc))
        {
          acc = new EventTypeOddsAccumulator
          {
            EventTypeName = row.EventTypeEntity.Name
          };
          byEventType[row.EventTypeId] = acc;
        }

        BookmakerEvent? ev = JsonSerializer.Deserialize<BookmakerEvent>(row.EventJson, _serializerOptions);

        if (ev == null)
        {
          continue;
        }

        if (acc.Title == null)
        {
          acc.Title = ev.Title;
        }
        if (acc.OptionOrder.Count == 0)
        {
          acc.OptionOrder.AddRange(ev.Options.Select(o => o.Label));
        }

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
          acc.OddsByLabel.TryGetValue(label, out var o) ? o : Array.Empty<(double, DateTime)>());
        return new OutcomePriceTimeline(label, segments);
      }).ToList();
      return new MarketPriceHistory(acc.EventTypeName, acc.Title, options);
    }).ToList();

    return sections;
  }


  [KernelFunction("GetClubRollingPerformance")]
  [Description("Gets performance data for a club from its latest 5 games: top players with recent ratings and average rating, team performances (recent team ratings and average), and formations used in each match.")]
  public async Task<TeamPerformanceResult?> GetClubRollingPerformanceAsync(int clubId, CancellationToken cancellationToken = default)
  {
    var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken).ConfigureAwait(false);
    if (club == null)
      return null;

    var matches = await _unitOfWork.Matches.GetRecentMatchesForClubAsync(clubId, 5, cancellationToken).ConfigureAwait(false);
    if (matches.Count == 0)
      return new TeamPerformanceResult(TopPlayers: Array.Empty<PlayerRecentRatings>(), RecentTeamRatings: Array.Empty<double>(), AvgTeamRating: 0, Formations: Array.Empty<string>());

    var matchesByDateAsc = matches.OrderBy(m => m.MatchDate).ToList();
    var playerToRatingsAndDates = new Dictionary<string, List<(double Rating, DateTime MatchDate)>>(StringComparer.Ordinal);
    var teamRatingsAndDates = new List<(double Rating, DateTime MatchDate)>();
    var formationsByDate = new List<(string Formation, DateTime MatchDate)>();

    foreach (var match in matchesByDateAsc)
    {
      var details = await _unitOfWork.Matches.GetMatchDetailsByMatchIdAsync(match.Id, cancellationToken).ConfigureAwait(false);
      var payload = details?.GetFotmobDetails();
      if (payload == null)
        continue;

      var lineup = match.HomeClubId == clubId ? payload.HomeLineup : payload.AwayLineup;
      if (lineup == null)
        continue;

      if (lineup.TeamRating.HasValue)
        teamRatingsAndDates.Add((lineup.TeamRating.Value, match.MatchDate));
      formationsByDate.Add((lineup.Formation ?? string.Empty, match.MatchDate));

      if (lineup.Players == null)
        continue;

      foreach (var p in lineup.Players)
      {
        if (!p.Rating.HasValue)
          continue;
        var name = p.Name;
        if (string.IsNullOrWhiteSpace(name))
          continue;
        if (!playerToRatingsAndDates.TryGetValue(name, out var list))
        {
          list = new List<(double, DateTime)>();
          playerToRatingsAndDates[name] = list;
        }
        list.Add((p.Rating.Value, match.MatchDate));
      }
    }

    var recentTeamRatings = teamRatingsAndDates.OrderBy(x => x.MatchDate).Select(x => x.Rating).ToList();
    var avgTeamRating = recentTeamRatings.Count > 0 ? Math.Round(recentTeamRatings.Average(), 2) : 0d;
    var formations = formationsByDate.OrderBy(x => x.MatchDate).Select(x => x.Formation).ToList();

    var topPlayers = playerToRatingsAndDates
      .Select(kv =>
      {
        var sorted = kv.Value.OrderBy(x => x.MatchDate).ToList();
        var recentRatings = sorted.Select(x => x.Rating).ToList();
        var avgRating = recentRatings.Count > 0 ? recentRatings.Average() : 0d;
        return new PlayerRecentRatings(Player: kv.Key, RecentRatings: recentRatings, AvgRating: Math.Round(avgRating, 2));
      })
      .OrderByDescending(p => p.AvgRating)
      .ToList();

    return new TeamPerformanceResult(TopPlayers: topPlayers, RecentTeamRatings: recentTeamRatings, AvgTeamRating: avgTeamRating, Formations: formations);
  }

  private static IReadOnlyList<PricePoint> CollapseToSegments(IReadOnlyList<(double Odds, DateTime At)> points)
  {
    if (points.Count == 0)
      return Array.Empty<PricePoint>();

    var sorted = points.OrderBy(p => p.At).ToList();
    var segments = new List<PricePoint>();
    var startTime = sorted[0].At;
    var currentOdds = sorted[0].Odds;

    for (var i = 1; i < sorted.Count; i++)
    {
      if (sorted[i].Odds != currentOdds)
      {
        segments.Add(new PricePoint(currentOdds, startTime, sorted[i].At));
        currentOdds = sorted[i].Odds;
        startTime = sorted[i].At;
      }
    }
    segments.Add(new PricePoint(currentOdds, startTime, EffectiveTo: null));
    return segments;
  }
}
