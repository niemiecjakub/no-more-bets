using System.Globalization;
using MediatR;
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

    var upcomingLeagueMatches = await _mediator.Send(new GetSoccerDataMatchPreviewsUpcomingQuery(leagueId), cancellationToken).ConfigureAwait(false);

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

      var lineup = _matchMatcher.FindLineup(game.HomeTeam, game.AwayTeam, lineupIndex) ?? GameLineup.Empty(game);
      var headToHead = await _mediator
           .Send(new GetSoccerDataHeadToHeadQuery(soccerdataMatch.Teams.Home.Id, soccerdataMatch.Teams.Away.Id), cancellationToken)
           .ConfigureAwait(false);

      var matchPreview = await _mediator.Send(new GetSoccerDataMatchPreviewQuery(soccerdataMatch.Id), cancellationToken)
          .ConfigureAwait(false);

      var events = await _mediator
          .Send(new GetBetclicMatchEventsQuery(game.Url, Expand: true), cancellationToken)
          .ConfigureAwait(false);

      var analysis = new Model.MatchAnalysis
      {
        Game = $"{soccerdataMatch.Teams.Home.Name} vs {soccerdataMatch.Teams.Away.Name}",
        Date = DateTime.Parse($"{soccerdataMatch.Date} {soccerdataMatch.Time}", CultureInfo.GetCultureInfo("en-GB")),
        HomeTeam = new MatchTeamData
        {
          Name = soccerdataMatch.Teams.Home.Name,
          Lineup = MapTeamLineup(lineup.HomeTeam),
          LeagueStatistics = GetTeamData(fotmobClubs, game.HomeTeam),
        },
        AwayTeam = new MatchTeamData
        {
          Name = soccerdataMatch.Teams.Away.Name,
          Lineup = MapTeamLineup(lineup.AwayTeam),
          LeagueStatistics = GetTeamData(fotmobClubs, game.AwayTeam)
        },
        HeadToHead = MapHeadToHead(headToHead),
        Preview = MapMatchPreview(matchPreview),
        Betting = MapBettingEvents(events)
      };
      results.Add(analysis);
    }

    if (_persistence != null)
    {
      await _persistence.SaveResultsAsync(results, CancellationToken.None).ConfigureAwait(false);
    }

    return results;
  }

  private TeamLeagueStats? GetTeamData(IReadOnlyList<ClubDto> fotmobClubs, string clubName)
  {
    var homeClub = _matchMatcher.FindFotmobClub(clubName, fotmobClubs);
    if (homeClub != null)
    {
      return MapTeamLeagueData(homeClub);
    }

    return null;
  }

  private static TeamLineupData MapTeamLineup(TeamLineup tl)
  {
    return new TeamLineupData
    {
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
    var s = h2h.Stats;

    return new HeadToHeadData
    {
      Team1 = new TeamMatchup
      {
        Info = new Model.TeamInfo { Id = h2h.Team1.Id, Name = h2h.Team1.Name },
        H2HStats = new H2HStats
        {
          Total = new StatSummary
          {
            Wins = s.Overall.OverallTeam1Wins,
            Draws = s.Overall.OverallDraws,
            Losses = s.Overall.OverallTeam2Wins, // Team 2 wins are Team 1 losses
            GoalsScored = s.Overall.OverallTeam1Scored
          },
          AtHome = new StatSummary
          {
            Wins = s.Team1AtHome.Team1WinsAtHome,
            Draws = s.Team1AtHome.Team1DrawsAtHome,
            Losses = s.Team1AtHome.Team1LossesAtHome,
            GoalsScored = s.Team1AtHome.Team1ScoredAtHome
          }
        }
      },
      Team2 = new TeamMatchup
      {
        Info = new Model.TeamInfo { Id = h2h.Team2.Id, Name = h2h.Team2.Name },
        H2HStats = new H2HStats
        {
          Total = new StatSummary
          {
            Wins = s.Overall.OverallTeam2Wins,
            Draws = s.Overall.OverallDraws,
            Losses = s.Overall.OverallTeam1Wins, // Team 1 wins are Team 2 losses
            GoalsScored = s.Overall.OverallTeam2Scored
          },
          AtHome = new StatSummary
          {
            Wins = s.Team2AtHome.Team2WinsAtHome,
            Draws = s.Team2AtHome.Team2DrawsAtHome,
            Losses = s.Team2AtHome.Team2LossesAtHome,
            GoalsScored = s.Team2AtHome.Team2ScoredAtHome
          }
        }
      }
    };
  }

  private static IReadOnlyList<Model.PreviewContentItem> MapMatchPreview(MatchPreview mp)
  {
    return mp.PreviewContent.Select(p => new Model.PreviewContentItem
    {
      Name = p.Name,
      Content = p.Content
    })
    .ToList()
    .AsReadOnly();
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
      Options = e.Options.Select(o => new BettingOptionInfo
      {
        Label = o.Label,
        Odds = o.Odds
      }).ToList()
    })
    .ToList()
    .AsReadOnly();
  }

  private static TeamLeagueStats MapTeamLeagueData(ClubDto club)
  {
    return new TeamLeagueStats
    {
      CurrentPostition = club.Position,
      MatchesPlayed = club.MatchesPlayed,
      Wins = club.Wins,
      Draws = club.Draws,
      Losses = club.Losses,
      GoalsFor = club.GoalsFor,
      GoalsAgainst = club.GoalsAgainst,
      GoalDifference = club.GoalDifference,
      Points = club.Points,
      Form = club.Form,
    };
  }
}
