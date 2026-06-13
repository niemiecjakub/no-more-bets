using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Leagues.GetLeagueTable;
using NoMoreBets.Application.Leagues.GetMatchGroupTable;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using LineupPlayer = NoMoreBets.Application.Matches.GetMatchLineups.Player;
using ToolTeamLineup = NoMoreBets.Infrastructure.AI.Tools.Implementations.Models.TeamLineup;
using ToolPlayer = NoMoreBets.Infrastructure.AI.Tools.Implementations.Models.Player;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class MatchToolTests
{
  private const int MatchId = 42;
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly MatchTool _sut;

  public MatchToolTests()
  {
    _unitOfWork.Matches.Returns(_matchRepository);
    _sut = new MatchTool(_unitOfWork, _mediator, new AgentSessionContext());
  }

  [Fact]
  public async Task GetLineupsAsync_WhenCalled_DispatchesGetMatchLineupsQuery()
  {
    var homePlayers = new List<LineupPlayer> { new("Alice", "GK") };
    var awayPlayers = new List<LineupPlayer> { new("Bob", "FW") };
    var lineupResult = new MatchLineupResult(
      new TeamLineupResult("Confirmed", homePlayers),
      new TeamLineupResult("Predicted", awayPlayers));
    _mediator.Send(Arg.Any<GetMatchLineupsQuery>(), Arg.Any<CancellationToken>()).Returns(lineupResult);

    var result = await _sut.GetLineupsAsync(MatchId);

    result.Should().BeEquivalentTo(new MatchLineup(
      new ToolTeamLineup([new ToolPlayer("Alice", "GK")]),
      new ToolTeamLineup([new ToolPlayer("Bob", "FW")])));
    await _mediator.Received(1).Send(Arg.Is<GetMatchLineupsQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenCalled_DispatchesGetMatchBettingOddsHistoryQuery()
  {
    var expected = new List<MarketPriceHistory> { new("Match Result", "1X2", []) };
    _mediator.Send(Arg.Any<GetMatchBettingOddsHistoryQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    var result = await _sut.GetMatchBettingOddsHistoryAsync(MatchId);

    result.Should().BeSameAs(expected);
    await _mediator.Received(1).Send(Arg.Is<GetMatchBettingOddsHistoryQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenCalled_DispatchesGetClubRollingPerformanceQueryWithNullDate()
  {
    TeamPerformanceResult? expected = null;
    _mediator.Send(Arg.Any<GetClubRollingPerformanceQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    var result = await _sut.GetClubRollingPerformanceAsync(7);

    result.Should().BeNull();
    await _mediator.Received(1).Send(
      Arg.Is<GetClubRollingPerformanceQuery>(q => q.ClubId == 7 && q.Date == null),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetInjuriesAsync_WhenCalled_DispatchesGetMatchInjuriesQuery()
  {
    _mediator.Send(Arg.Any<GetMatchInjuriesQuery>(), Arg.Any<CancellationToken>())
      .Returns(new MatchInjuriesResult(new TeamInjuriesResult([]), new TeamInjuriesResult([])));

    await _sut.GetInjuriesAsync(MatchId);

    await _mediator.Received(1).Send(Arg.Is<GetMatchInjuriesQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenCalled_DispatchesGetHeadToHeadStatsQuery()
  {
    _mediator.Send(Arg.Any<GetHeadToHeadStatsQuery>(), Arg.Any<CancellationToken>())
      .Returns(new H2H { Summary = "a", TeamA = new TeamMetrics { Name = "a" }, TeamB = new TeamMetrics { Name = "b" } });

    await _sut.GetHead2HeadStatsAsync(MatchId);

    await _mediator.Received(1).Send(Arg.Is<GetHeadToHeadStatsQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenCalled_DispatchesGetClubRecentGamesQuery()
  {
    _mediator.Send(Arg.Any<GetClubRecentGamesQuery>(), Arg.Any<CancellationToken>()).Returns(new List<RecentMatch>());

    await _sut.GetClubRecentGamesAsync(5);

    await _mediator.Received(1).Send(Arg.Is<GetClubRecentGamesQuery>(q => q.ClubId == 5 && q.Date == null), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubStatistics_WhenCalled_DispatchesGetClubLeagueStatisticsQueryWithNullDate()
  {
    _mediator.Send(Arg.Any<GetClubLeagueStatisticsQuery>(), Arg.Any<CancellationToken>()).Returns((ClubLeagueStats?)null);

    await _sut.GetClubStatistics(11);

    await _mediator.Received(1).Send(
      Arg.Is<GetClubLeagueStatisticsQuery>(q => q.ClubId == 11 && q.Date == null),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubDailySummaryAsync_WhenCalled_DispatchesGetClubDailySummaryQueryWithNullDate()
  {
    _mediator.Send(Arg.Any<GetClubDailySummaryQuery>(), Arg.Any<CancellationToken>()).Returns("summary");

    await _sut.GetClubDailySummaryAsync(9);

    await _mediator.Received(1).Send(
      Arg.Is<GetClubDailySummaryQuery>(q => q.ClubId == 9 && q.Date == null),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetLeagueTableAsync_WhenMatchHasLeague_DispatchesGetLeagueTableQuery()
  {
    var league = new League { Id = 99, Name = "L", Slug = "l", SoccerdataId = 1 };
    var season = new Season { Id = 1, LeagueId = 99, Year = "2025", League = league };
    var stage = new Stage { Id = 1, SeasonId = 1, Name = "S", Season = season };
    var match = new Match
    {
      Id = MatchId,
      MatchDate = new DateTime(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc),
      Stage = stage
    };
    _matchRepository.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    var expected = new List<LeagueTableStanding>();
    _mediator.Send(Arg.Any<GetLeagueTableQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    var result = await _sut.GetLeagueTableAsync(MatchId);

    result.Should().BeSameAs(expected);
    await _matchRepository.Received(1).GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>());
    await _mediator.Received(1).Send(
      Arg.Is<GetLeagueTableQuery>(q => q.LeagueId == 99 && q.AsOfDate == null),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetLeagueTableAsync_WhenMatchMissing_ReturnsNull()
  {
    _matchRepository.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns((Match?)null);

    var result = await _sut.GetLeagueTableAsync(MatchId);

    result.Should().BeNull();
    await _mediator.DidNotReceive().Send(Arg.Any<GetLeagueTableQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetGroupTableAsync_WhenCalled_DispatchesGetMatchGroupTableQuery()
  {
    var expected = new List<LeagueTableStanding>();
    _mediator.Send(Arg.Any<GetMatchGroupTableQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    var result = await _sut.GetGroupTableAsync(MatchId);

    result.Should().BeSameAs(expected);
    await _mediator.Received(1).Send(Arg.Is<GetMatchGroupTableQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }
}
