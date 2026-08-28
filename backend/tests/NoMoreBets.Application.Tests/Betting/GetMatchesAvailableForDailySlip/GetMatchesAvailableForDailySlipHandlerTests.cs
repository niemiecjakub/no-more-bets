using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchesAvailableForDailySlip;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetMatchesAvailableForDailySlip;

public class GetMatchesAvailableForDailySlipHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly GetMatchesAvailableForDailySlipHandler _sut;

  public GetMatchesAvailableForDailySlipHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetMatchesAvailableForDailySlipHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_KeepsMatchesKickingOffOnWarsawToday_AndDropsTheNextDay()
  {
    // Arrange — 06:00 UTC 28 Aug is still 28 Aug in Warsaw
    var utcNow = new DateTime(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc);
    var todayKickoff = new DateTime(2026, 8, 28, 18, 0, 0, DateTimeKind.Utc);
    var afterMidnightKickoff = new DateTime(2026, 8, 28, 22, 30, 0, DateTimeKind.Utc);

    var todayMatch = new Match { Id = 1, MatchDate = todayKickoff, HomeClub = new ClubEntity { Name = "A" }, AwayClub = new ClubEntity { Name = "B" } };
    var nextDayMatch = new Match { Id = 2, MatchDate = afterMidnightKickoff, HomeClub = new ClubEntity { Name = "C" }, AwayClub = new ClubEntity { Name = "D" } };

    _betting.GetMatchesAvailableForBettingAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { todayMatch, nextDayMatch });

    // Act
    var result = await _sut.Handle(new GetMatchesAvailableForDailySlipQuery(utcNow), CancellationToken.None);

    // Assert
    result.Should().ContainSingle().Which.Id.Should().Be(1);
  }
}
