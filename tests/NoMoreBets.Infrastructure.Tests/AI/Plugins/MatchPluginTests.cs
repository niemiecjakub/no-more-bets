using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Application.Matches.GetMatchPreview;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class MatchPluginTests
{
  private const int MatchId = 42;
  private readonly Match _match;
  private readonly IMediator _mediator;
  private readonly MatchPlugin _sut;

  public MatchPluginTests()
  {
    _match = new Match { Id = MatchId, MatchDate = new DateTime(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc) };
    _mediator = Substitute.For<IMediator>();
    _sut = new MatchPlugin(_match, _mediator);
  }

  [Fact]
  public async Task GetLineupsAsync_WhenCalled_DispatchesGetMatchLineupsQuery()
  {
    // Arrange
    var expected = new MatchLineupResult(new TeamLineupResult("Confirmed", []), new TeamLineupResult("Predicted", []));
    _mediator.Send(Arg.Any<GetMatchLineupsQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    // Act
    var result = await _sut.GetLineupsAsync();

    // Assert
    result.Should().BeSameAs(expected);
    await _mediator.Received(1).Send(Arg.Is<GetMatchLineupsQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenCalled_DispatchesGetMatchBettingOddsHistoryQuery()
  {
    // Arrange
    var expected = new List<MarketPriceHistory> { new("Match Result", "1X2", []) };
    _mediator.Send(Arg.Any<GetMatchBettingOddsHistoryQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().BeSameAs(expected);
    await _mediator.Received(1).Send(Arg.Is<GetMatchBettingOddsHistoryQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenCalled_DispatchesGetClubRollingPerformanceQueryWithMatchDate()
  {
    // Arrange
    var matchDate = new DateOnly(2026, 3, 1);
    var expected = new TeamPerformanceResult([], [], 0, []);
    _mediator.Send(Arg.Any<GetClubRollingPerformanceQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(7);

    // Assert
    result.Should().BeSameAs(expected);
    await _mediator.Received(1).Send(
      Arg.Is<GetClubRollingPerformanceQuery>(q => q.ClubId == 7 && q.Date == matchDate),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetInjuriesAsync_WhenCalled_DispatchesGetMatchInjuriesQuery()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchInjuriesQuery>(), Arg.Any<CancellationToken>())
      .Returns(new MatchInjuriesResult(new TeamInjuriesResult([]), new TeamInjuriesResult([])));

    // Act
    await _sut.GetInjuriesAsync();

    // Assert
    await _mediator.Received(1).Send(Arg.Is<GetMatchInjuriesQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenCalled_DispatchesGetMatchPreviewQuery()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchPreviewQuery>(), Arg.Any<CancellationToken>()).Returns("preview");

    // Act
    await _sut.GetMatchPreviewAsync();

    // Assert
    await _mediator.Received(1).Send(Arg.Is<GetMatchPreviewQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenCalled_DispatchesGetHeadToHeadStatsQuery()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetHeadToHeadStatsQuery>(), Arg.Any<CancellationToken>())
      .Returns(new H2H { Summary = "a", TeamA = new TeamMetrics { Name = "a" }, TeamB = new TeamMetrics { Name = "b" } });

    // Act
    await _sut.GetHead2HeadStatsAsync();

    // Assert
    await _mediator.Received(1).Send(Arg.Is<GetHeadToHeadStatsQuery>(q => q.MatchId == MatchId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenCalled_DispatchesGetClubRecentGamesQuery()
  {
    // Arrange
    var matchDate = new DateOnly(2026, 3, 1);
    _mediator.Send(Arg.Any<GetClubRecentGamesQuery>(), Arg.Any<CancellationToken>()).Returns(new List<RecentMatch>());

    // Act
    await _sut.GetClubRecentGamesAsync(5);

    // Assert
    await _mediator.Received(1).Send(Arg.Is<GetClubRecentGamesQuery>(q => q.ClubId == 5 && q.Date == matchDate), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubStatistics_WhenCalled_DispatchesGetClubLeagueStatisticsQueryWithMatchDate()
  {
    // Arrange
    var matchDate = new DateOnly(2026, 3, 1);
    _mediator.Send(Arg.Any<GetClubLeagueStatisticsQuery>(), Arg.Any<CancellationToken>()).Returns((ClubLeagueStats?)null);

    // Act
    await _sut.GetClubStatistics(11);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<GetClubLeagueStatisticsQuery>(q => q.ClubId == 11 && q.Date == matchDate),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetClubDailySummaryAsync_WhenCalled_DispatchesGetClubDailySummaryQueryWithMatchDate()
  {
    // Arrange
    var matchDate = new DateOnly(2026, 3, 1);
    _mediator.Send(Arg.Any<GetClubDailySummaryQuery>(), Arg.Any<CancellationToken>()).Returns("summary");

    // Act
    await _sut.GetClubDailySummaryAsync(9);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<GetClubDailySummaryQuery>(q => q.ClubId == 9 && q.Date == matchDate),
      Arg.Any<CancellationToken>());
  }
}
