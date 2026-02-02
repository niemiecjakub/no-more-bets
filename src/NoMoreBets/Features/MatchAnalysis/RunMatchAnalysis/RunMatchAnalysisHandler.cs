using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.MatchAnalysis.Model;
using NoMoreBets.Features.MatchAnalysis.Options;
using NoMoreBets.Features.MatchAnalysis.Persistence;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.MatchAnalysis.RunMatchAnalysis;

/// <summary>
/// Orchestrates match data collection from Rotowire, SoccerData, Betclic, FotMob and builds MatchAnalysis per game.
/// Maps all feature types into MatchAnalysis-owned models only.
/// </summary>
public sealed class RunMatchAnalysisHandler : IRequestHandler<RunMatchAnalysisQuery, IReadOnlyList<Model.MatchAnalysis>>
{
  private readonly IMediator _mediator;
  private readonly IMatchMatcher _matchMatcher;
  private readonly MatchAnalysisOptions _options;
  private readonly IMatchAnalysisPersistence? _persistence;
  private readonly ILogger<RunMatchAnalysisHandler> _logger;

  public RunMatchAnalysisHandler(
      IMediator mediator,
      IMatchMatcher matchMatcher,
      IOptions<MatchAnalysisOptions> options,
      ILogger<RunMatchAnalysisHandler> logger,
      IMatchAnalysisPersistence? persistence = null)
  {
    _mediator = mediator;
    _matchMatcher = matchMatcher;
    _options = options.Value;
    _logger = logger;
    _persistence = persistence;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<Model.MatchAnalysis>> Handle(RunMatchAnalysisQuery request, CancellationToken cancellationToken)
  {
    var leagueId = request.LeagueId ?? _options.LeagueId;

    var lineups = await _mediator.Send(new GetRotowireLineupsQuery(), cancellationToken).ConfigureAwait(false);
    var lineupIndex = _matchMatcher.BuildLineupIndex(lineups);

    var upcomingLeagueMatches = await _mediator
        .Send(new GetSoccerDataMatchPreviewsUpcomingQuery(leagueId), cancellationToken)
        .ConfigureAwait(false);

    var fotmobClubs = await _mediator.Send(new GetFotmobLeagueTableQuery(), cancellationToken).ConfigureAwait(false);

    var bookmakerGames = await _mediator.Send(new GetBetclicUpcomingGamesQuery(), cancellationToken).ConfigureAwait(false);

    var results = new List<Model.MatchAnalysis>(bookmakerGames.Count);
    foreach (var game in bookmakerGames)
    {
      var soccerdataMatch = _matchMatcher.FindSoccerDataMatch(game.HomeTeam, game.AwayTeam, upcomingLeagueMatches);
      if (soccerdataMatch == null)
      {
        _logger.LogInformation("No SoccerData match found for {Home} vs {Away}; skipping match analysis", game.HomeTeam, game.AwayTeam);
        continue;
      }

      var lineup = _matchMatcher.FindLineup(game.HomeTeam, game.AwayTeam, lineupIndex);
      var headToHead = await _mediator
           .Send(new GetSoccerDataHeadToHeadQuery(soccerdataMatch.Teams.Home.Id, soccerdataMatch.Teams.Away.Id), cancellationToken)
           .ConfigureAwait(false);

      var matchPreview = await _mediator.Send(new GetSoccerDataMatchPreviewQuery(soccerdataMatch.Id), cancellationToken)
          .ConfigureAwait(false);

      var events = await _mediator
          .Send(new GetBetclicMatchEventsQuery(game.Url, Expand: true), cancellationToken)
          .ConfigureAwait(false);

      var homeClub = _matchMatcher.FindFotmobClub(game.HomeTeam, fotmobClubs);
      var awayClub = _matchMatcher.FindFotmobClub(game.AwayTeam, fotmobClubs);
      var fbrefHome = homeClub != null ? MapFbrefTeamData(homeClub) : null;
      var fbrefAway = awayClub != null ? MapFbrefTeamData(awayClub) : null;

      var output = new
      {
        Game = $"{soccerdataMatch.Teams.Home.Name} vs {soccerdataMatch.Teams.Away.Name}",
        Date = DateTime.Parse($"{soccerdataMatch.Date} {soccerdataMatch.Time}"),
        Teams = new
        {
          Home = new
          {
            Lineup = MapTeamLineup(lineup.HomeTeam),
          },
          Away = new
          {
            Lineup = MapTeamLineup(lineup.AwayTeam),
          }
        },
        Statistics = new
        {
          HeadToHead = MapHeadToHead(headToHead),
        },
        Preview = MapMatchPreview(matchPreview),
        Betting = MapBettingEvents(events)
      };

      var analysis = await CollectMatchDataAsync(
          game,
          lineupIndex,
          upcomingLeagueMatches,
          fotmobClubs,
          cancellationToken).ConfigureAwait(false);
      results.Add(analysis);
    }

    if (_persistence != null)
    {
      await _persistence.SaveResultsAsync(results, CancellationToken.None).ConfigureAwait(false);
    }

    return results;
  }

  private async Task<Model.MatchAnalysis> CollectMatchDataAsync(
      UpcomingGame game,
      IReadOnlyDictionary<TeamKey, GameLineup> lineupIndex,
      IReadOnlyList<LeagueMatchPreviews> upcomingLeagueMatches,
      IReadOnlyList<ClubDto> fotmobClubs,
      CancellationToken cancellationToken)
  {
    var matchInfo = new MatchInfo
    {
      Home = game.HomeTeam,
      Away = game.AwayTeam,
      Date = game.Date,
      Time = game.Time
    };

    LineupData? lineupData = null;
    var lineup = _matchMatcher.FindLineup(game.HomeTeam, game.AwayTeam, lineupIndex);
    if (lineup != null)
    {
      lineupData = new LineupData
      {
        Home = MapTeamLineup(lineup.HomeTeam),
        Away = MapTeamLineup(lineup.AwayTeam)
      };
    }

    HeadToHeadData? headToHeadData = null;
    MatchPreviewData? matchPreviewData = null;
    int? matchId = null;

    var soccerdataMatch = _matchMatcher.FindSoccerDataMatch(game.HomeTeam, game.AwayTeam, upcomingLeagueMatches);
    if (soccerdataMatch != null)
    {
      matchId = soccerdataMatch.Id;
      try
      {
        var headToHead = await _mediator
            .Send(new GetSoccerDataHeadToHeadQuery(soccerdataMatch.Teams.Home.Id, soccerdataMatch.Teams.Away.Id), cancellationToken)
            .ConfigureAwait(false);
        headToHeadData = MapHeadToHead(headToHead);

        var matchPreview = await _mediator
            .Send(new GetSoccerDataMatchPreviewQuery(soccerdataMatch.Id), cancellationToken)
            .ConfigureAwait(false);
        matchPreviewData = MapMatchPreview(matchPreview);
      }
      catch (OperationCanceledException)
      {
        _logger.LogWarning("SoccerData request was canceled for {Home} vs {Away}; head-to-head and match preview will be missing", game.HomeTeam, game.AwayTeam);
      }
    }

    var events = await _mediator
        .Send(new GetBetclicMatchEventsQuery(game.Url, Expand: true), cancellationToken)
        .ConfigureAwait(false);

    IReadOnlyList<BettingEventInfo>? bettingEvents = MapBettingEvents(events);

    var homeClub = _matchMatcher.FindFotmobClub(game.HomeTeam, fotmobClubs);
    var awayClub = _matchMatcher.FindFotmobClub(game.AwayTeam, fotmobClubs);
    var fbrefHome = homeClub != null ? MapFbrefTeamData(homeClub) : null;
    var fbrefAway = awayClub != null ? MapFbrefTeamData(awayClub) : null;

    return new Model.MatchAnalysis
    {
      MatchInfo = matchInfo,
      Lineup = lineupData,
      HeadToHead = headToHeadData,
      MatchPreview = matchPreviewData,
      BettingEvents = bettingEvents,
      FbrefHome = fbrefHome,
      FbrefAway = fbrefAway,
      MatchId = matchId
    };
  }

  private static TeamLineupData MapTeamLineup(TeamLineup tl)
  {
    return new TeamLineupData
    {
      TeamName = tl.TeamName,
      LineupTypeDisplayName = LineupTypes.GetDisplayName(tl.LineupType),
      Players = tl.Players.Select(p => new PlayerInLineupInfo
      {
        Position = FootballPositions.GetFullName(p.Position),
        Player = p.Player
      }).ToList(),
      Injuries = tl.Injuries.Select(i => new InjuryInfo
      {
        Position = FootballPositions.GetFullName(i.Position),
        Player = i.Player,
        Status = InjuryStatuses.GetFullName(i.Status)
      }).ToList()
    };
  }

  private static HeadToHeadData MapHeadToHead(HeadToHead h2h)
  {
    return new HeadToHeadData
    {
      Team1 = new Model.TeamInfo { Id = h2h.Team1.Id, Name = h2h.Team1.Name },
      Team2 = new Model.TeamInfo { Id = h2h.Team2.Id, Name = h2h.Team2.Name },
      Overall = new Model.OverallStats
      {
        OverallGamesPlayed = h2h.Stats.Overall.OverallGamesPlayed,
        OverallTeam1Wins = h2h.Stats.Overall.OverallTeam1Wins,
        OverallTeam2Wins = h2h.Stats.Overall.OverallTeam2Wins,
        OverallDraws = h2h.Stats.Overall.OverallDraws,
        OverallTeam1Scored = h2h.Stats.Overall.OverallTeam1Scored,
        OverallTeam2Scored = h2h.Stats.Overall.OverallTeam2Scored
      },
      Team1AtHome = new Model.Team1AtHomeStats
      {
        Team1GamesPlayedAtHome = h2h.Stats.Team1AtHome.Team1GamesPlayedAtHome,
        Team1WinsAtHome = h2h.Stats.Team1AtHome.Team1WinsAtHome,
        Team1LossesAtHome = h2h.Stats.Team1AtHome.Team1LossesAtHome,
        Team1DrawsAtHome = h2h.Stats.Team1AtHome.Team1DrawsAtHome,
        Team1ScoredAtHome = h2h.Stats.Team1AtHome.Team1ScoredAtHome,
        Team1ConcededAtHome = h2h.Stats.Team1AtHome.Team1ConcededAtHome
      },
      Team2AtHome = new Model.Team2AtHomeStats
      {
        Team2GamesPlayedAtHome = h2h.Stats.Team2AtHome.Team2GamesPlayedAtHome,
        Team2WinsAtHome = h2h.Stats.Team2AtHome.Team2WinsAtHome,
        Team2LossesAtHome = h2h.Stats.Team2AtHome.Team2LossesAtHome,
        Team2DrawsAtHome = h2h.Stats.Team2AtHome.Team2DrawsAtHome,
        Team2ScoredAtHome = h2h.Stats.Team2AtHome.Team2ScoredAtHome,
        Team2ConcededAtHome = h2h.Stats.Team2AtHome.Team2ConcededAtHome
      }
    };
  }

  private static MatchPreviewData MapMatchPreview(MatchPreview mp)
  {
    var pred = mp.MatchData.Prediction;
    var teamName = pred.Choice switch
    {
      "home" => mp.Teams.Home.Name,
      "away" => mp.Teams.Away.Name,
      _ => pred.Choice
    };
    return new MatchPreviewData
    {
      ExcitementRating = mp.MatchData.ExcitementRating,
      Prediction = new PredictionData { Type = pred.Type, Choice = pred.Choice, TeamName = teamName },
      Weather = new WeatherData
      {
        Description = mp.MatchData.Weather.Description,
        TempC = mp.MatchData.Weather.TempC,
        TempF = mp.MatchData.Weather.TempF
      },
      PreviewContent = mp.PreviewContent.Select(p => new Model.PreviewContentItem { Name = p.Name, Content = p.Content }).ToList()
    };
  }

  private IReadOnlyList<BettingEventInfo> MapBettingEvents(IEnumerable<BookmakerEvent> events)
  {
    if (events == null)
    {
      return new List<BettingEventInfo>();
    }

    return events.Select(e => new BettingEventInfo
    {
      Title = e.Title,
      Options = e.Options.Select(o => new BettingOptionInfo { Label = o.Label, Odds = o.Odds }).ToList()
    })
    .ToList()
    .AsReadOnly();
  }

  private static FbrefTeamData MapFbrefTeamData(ClubDto club)
  {
    return new FbrefTeamData
    {
      ClubStats = new TeamLeagueStats
      {
        Position = club.Position,
        TeamName = club.TeamName,
        TeamShortname = club.TeamShortname,
        TeamId = club.TeamId,
        TeamLogoUrl = club.TeamLogoUrl,
        MatchesPlayed = club.MatchesPlayed,
        Wins = club.Wins,
        Draws = club.Draws,
        Losses = club.Losses,
        GoalsFor = club.GoalsFor,
        GoalsAgainst = club.GoalsAgainst,
        GoalDifference = club.GoalDifference,
        Points = club.Points,
        Form = club.Form,
        NextOpponentId = club.NextOpponentId,
        NextOpponentName = club.NextOpponentName,
        NextOpponentLogoUrl = club.NextOpponentLogoUrl
      },
      RecentGames = []
    };
  }
}
