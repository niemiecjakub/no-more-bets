//using System.Globalization;
//using MediatR;
//using Microsoft.Extensions.Options;
//using NoMoreBets.Domain.Enums;
//using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
//using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
//using NoMoreBets.Features.Betclic.Model;
//using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
//using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
//using NoMoreBets.Features.Fotmob.GetFotmobXgStats;
//using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
//using NoMoreBets.Features.Fotmob.Model;
//using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
//using NoMoreBets.Features.MatchAnalysis.Model;
//using NoMoreBets.Features.MatchAnalysis.Options;
//using NoMoreBets.Features.MatchAnalysis.Persistence;
//using NoMoreBets.Features.Rotowire.GetRotowireLineups;
//using NoMoreBets.Features.Rotowire.Model;
//using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
//using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
//using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
//using NoMoreBets.Features.SoccerData.Model;

//namespace NoMoreBets.Features.MatchAnalysis.RunMatchAnalysis;

///// <summary>
///// Orchestrates match data collection from Rotowire, SoccerData, Betclic, FotMob and builds MatchAnalysis per game.
///// Maps all feature types into MatchAnalysis-owned models only.
///// </summary>
//public sealed class RunMatchAnalysisHandler : IRequestHandler<RunMatchAnalysisQuery, IReadOnlyList<Model.MatchAnalysis>>
//{
//  private readonly IMediator _mediator;
//  private readonly IMatchMatcher _matchMatcher;
//  private readonly MatchAnalysisOptions _options;
//  private readonly IMatchAnalysisPersistence? _persistence;
//  private readonly ILogger<RunMatchAnalysisHandler> _logger;

//  public RunMatchAnalysisHandler(
//      IMediator mediator,
//      IMatchMatcher matchMatcher,
//      IOptions<MatchAnalysisOptions> options,
//      ILogger<RunMatchAnalysisHandler> logger,
//      IMatchAnalysisPersistence? persistence = null)
//  {
//    _mediator = mediator;
//    _matchMatcher = matchMatcher;
//    _options = options.Value;
//    _logger = logger;
//    _persistence = persistence;
//  }

//  /// <inheritdoc />
//  public async Task<IReadOnlyList<Model.MatchAnalysis>> Handle(RunMatchAnalysisQuery request, CancellationToken cancellationToken)
//  {
//    var leagueId = request.LeagueId ?? _options.LeagueId;

//    await _mediator.Send(new RefreshRotowireLineupsCommand(), cancellationToken).ConfigureAwait(false);
//    var lineups = await _mediator.Send(new GetRotowireLineupsQuery(), cancellationToken).ConfigureAwait(false);
//    var lineupIndex = _matchMatcher.BuildLineupIndex(lineups);

//    await _mediator.Send(new RefreshSoccerDataMatchPreviewsUpcomingCommand(leagueId), cancellationToken).ConfigureAwait(false);
//    var upcomingLeagueMatches = await _mediator.Send(new GetSoccerDataMatchPreviewsUpcomingQuery(leagueId), cancellationToken).ConfigureAwait(false)
//      ?? [];

//    var fotmobClubs = await _mediator.Send(new GetFotmobLeagueTableQuery(), cancellationToken).ConfigureAwait(false);

//    var xgStats = await _mediator.Send(new GetFotmobXgStatsQuery(), cancellationToken).ConfigureAwait(false);

//    var bookmakerGames = await _mediator.Send(new GetBetclicUpcomingGamesQuery(), cancellationToken).ConfigureAwait(false);

//    var results = new List<Model.MatchAnalysis>(bookmakerGames.Count);
//    foreach (var game in bookmakerGames)
//    {
//      var soccerdataMatch = _matchMatcher.FindSoccerDataMatch(game.HomeTeam.Name, game.AwayTeam.Name, upcomingLeagueMatches);
//      if (soccerdataMatch == null)
//      {
//        _logger.LogInformation("No SoccerData match found for {Home} vs {Away}; skipping match analysis", game.HomeTeam.Name, game.AwayTeam.Name);
//        continue;
//      }

//      var lineup = _matchMatcher.FindLineup(game.HomeTeam.Name, game.AwayTeam.Name, lineupIndex) ?? GameLineup.Empty(game);
//      await _mediator.Send(new RefreshSoccerDataHeadToHeadCommand(soccerdataMatch.Teams.Home.Id, soccerdataMatch.Teams.Away.Id), cancellationToken).ConfigureAwait(false);
//      var headToHead = await _mediator
//           .Send(new GetSoccerDataHeadToHeadQuery(soccerdataMatch.Teams.Home.Id, soccerdataMatch.Teams.Away.Id), cancellationToken)
//           .ConfigureAwait(false);

//      await _mediator.Send(new RefreshSoccerDataMatchPreviewCommand(soccerdataMatch.Id), cancellationToken).ConfigureAwait(false);
//      var matchPreview = await _mediator.Send(new GetSoccerDataMatchPreviewQuery(soccerdataMatch.Id), cancellationToken)
//          .ConfigureAwait(false);

//      var events = await _mediator
//          .Send(new GetBetclicMatchEventsQuery(game.Url, Expand: true), cancellationToken)
//          .ConfigureAwait(false);

//      var analysis = new Model.MatchAnalysis
//      {
//        Game = $"{soccerdataMatch.Teams.Home.Name} vs {soccerdataMatch.Teams.Away.Name}",
//        Date = DateTime.Parse($"{soccerdataMatch.Date} {soccerdataMatch.Time}", CultureInfo.GetCultureInfo("en-GB")),
//        Weather =  MapWeather(matchPreview?.MatchData?.Weather),
//        HomeTeam = new MatchTeamData
//        {
//          Name = soccerdataMatch.Teams.Home.Name,
//          Lineup = MapTeamLineup(lineup.HomeTeam),
//          LeagueStatistics = GetTeamData(fotmobClubs, game.HomeTeam.Name),
//          XgStats = MapXgStats(_matchMatcher.FindXgStats(game.HomeTeam.Name, xgStats)),
//        },
//        AwayTeam = new MatchTeamData
//        {
//          Name = soccerdataMatch.Teams.Away.Name,
//          Lineup = MapTeamLineup(lineup.AwayTeam),
//          LeagueStatistics = GetTeamData(fotmobClubs, game.AwayTeam.Name),
//          XgStats = MapXgStats(_matchMatcher.FindXgStats(game.AwayTeam.Name, xgStats)),
//        },
//        HeadToHead = headToHead is not null ? MapHeadToHead(headToHead) : null,
//        Preview = matchPreview is not null ? MapMatchPreview(matchPreview) : null,
//        Betting = MapBettingEvents(events)
//      };
//      results.Add(analysis);
//    }

//    if (_persistence != null)
//    {
//      await _persistence.SaveResultsAsync(results, CancellationToken.None).ConfigureAwait(false);
//    }

//    return results;
//  }

//  private TeamLeagueStats? GetTeamData(IReadOnlyList<ClubDto> fotmobClubs, string clubName)
//  {
//    var homeClub = _matchMatcher.FindFotmobClub(clubName, fotmobClubs);
//    if (homeClub != null)
//    {
//      return MapTeamLeagueData(homeClub);
//    }

//    return null;
//  }

//  private static TeamLineupData MapTeamLineup(Rotowire.Model.TeamLineup tl)
//  {
//    return new TeamLineupData
//    {
//      Type = LineupTypes.GetDisplayName(tl.LineupType),
//      Players = tl.Players.Select(p => new PlayerInLineupInfo
//      {
//        Position = FootballPositions.GetFullName(p.Position),
//        Name = p.Player
//      }).ToList(),
//      Injuries = tl.Injuries.Select(i => new PlayerInjuryInfo
//      {
//        Position = FootballPositions.GetFullName(i.Position),
//        Name = i.Player,
//        Status = InjuryStatuses.GetFullName(i.Status)
//      }).ToList()
//    };
//  }

//  private static HeadToHeadData MapHeadToHead(HeadToHead h2h)
//  {
//    var s = h2h.Stats;

//    return new HeadToHeadData
//    {
//      Home = new TeamMatchup
//      {
//        Name = h2h.Team1.Name,
//        H2HStats = new H2HStats
//        {
//          Total = new StatSummary
//          {
//            Wins = s.Overall.OverallTeam1Wins,
//            Draws = s.Overall.OverallDraws,
//            Losses = s.Overall.OverallTeam2Wins, // Team 2 wins are Team 1 losses
//            GoalsScored = s.Overall.OverallTeam1Scored
//          },
//          AtHome = new StatSummary
//          {
//            Wins = s.Team1AtHome.Team1WinsAtHome,
//            Draws = s.Team1AtHome.Team1DrawsAtHome,
//            Losses = s.Team1AtHome.Team1LossesAtHome,
//            GoalsScored = s.Team1AtHome.Team1ScoredAtHome
//          }
//        }
//      },
//      Away = new TeamMatchup
//      {
//        Name = h2h.Team2.Name,
//        H2HStats = new H2HStats
//        {
//          Total = new StatSummary
//          {
//            Wins = s.Overall.OverallTeam2Wins,
//            Draws = s.Overall.OverallDraws,
//            Losses = s.Overall.OverallTeam1Wins, // Team 1 wins are Team 2 losses
//            GoalsScored = s.Overall.OverallTeam2Scored
//          },
//          AtHome = new StatSummary
//          {
//            Wins = s.Team2AtHome.Team2WinsAtHome,
//            Draws = s.Team2AtHome.Team2DrawsAtHome,
//            Losses = s.Team2AtHome.Team2LossesAtHome,
//            GoalsScored = s.Team2AtHome.Team2ScoredAtHome
//          }
//        }
//      }
//    };
//  }

//  private static IReadOnlyList<string> MapMatchPreview(MatchPreview mp)
//  {
//    return mp.PreviewContent.Select(p => p.Content).ToList();
//  }

//  private static WeatherData? MapWeather(Weather? w)
//  {
//    if (w == null)
//    {
//      return null;
//    }

//    return new WeatherData
//    {
//      TempC = w.TempC,
//      Description = w.Description ?? string.Empty
//    };
//  }

//  private static TeamXgData? MapXgStats(XgStatsDto? dto)
//  {
//    if (dto == null)
//    {
//      return null;
//    }

//    return new TeamXgData
//    {
//      Xg = dto.Xg,
//      Xga = dto.Xga,
//      XgDiff = dto.XgDiff,
//      XgaDiff = dto.XgaDiff,
//    };
//  }

//  private IReadOnlyList<BettingEventInfo> MapBettingEvents(IEnumerable<BookmakerEvent> events)
//  {
//    if (events == null)
//    {
//      return new List<BettingEventInfo>();
//    }

//    return events.Select(e => new BettingEventInfo
//    {
//      Title = e.Title,
//      Options = e.Options.Select(o => new BettingOptionInfo
//      {
//        Label = o.Label,
//        Odds = o.Odds
//      }).ToList()
//    })
//    .ToList()
//    .AsReadOnly();
//  }

//  private static TeamLeagueStats MapTeamLeagueData(ClubDto club)
//  {
//    return new TeamLeagueStats
//    {
//      CurrentPostition = club.Position,
//      MatchesPlayed = club.MatchesPlayed,
//      Wins = club.Wins,
//      Draws = club.Draws,
//      Losses = club.Losses,
//      GoalsFor = club.GoalsFor,
//      GoalsAgainst = club.GoalsAgainst,
//      GoalDifference = club.GoalDifference,
//      Points = club.Points,
//      Form = club.Form.Select(s => Enum.Parse<MatchResult>(s)).ToList().AsReadOnly(),
//    };
//  }
//}
