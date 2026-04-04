using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetBetSlips;

public class GetBetSlipsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetBetSlipsHandler _sut;

  public GetBetSlipsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetBetSlipsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_PassesNullStatusToRepository_WhenQueryHasNoStatusFilter()
  {
    // Arrange
    _bettingRepository.GetBetSlipsAsync(null, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());

    // Act
    await _sut.Handle(new GetBetSlipsQuery(), CancellationToken.None);

    // Assert
    await _bettingRepository.Received(1).GetBetSlipsAsync(null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PassesStatusToRepository_WhenQueryFiltersByStatus()
  {
    // Arrange
    _bettingRepository.GetBetSlipsAsync(BetStatus.Pending, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());

    // Act
    await _sut.Handle(new GetBetSlipsQuery(BetStatus.Pending), CancellationToken.None);

    // Assert
    await _bettingRepository.Received(1).GetBetSlipsAsync(BetStatus.Pending, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_OrdersSelectionsByIdAscending_AndMapsDisplayNames()
  {
    // Arrange
    var match = new Match
    {
      Id = 9,
      HomeClub = new ClubEntity { Name = "Arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea" }
    };
    var slip = new BetSlip
    {
      Id = 1,
      CreatedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2.5m,
      PotentialPayout = 25m,
      BetStatus = BetStatus.Pending,
      Selections =
      [
        new BetSelection
        {
          Id = 2,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Away,
          OddsAtPlacement = 2.5m,
          BetStatus = BetStatus.Pending,
          Match = match
        },
        new BetSelection
        {
          Id = 1,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Home,
          OddsAtPlacement = 1.9m,
          BetStatus = BetStatus.Pending,
          Match = match
        }
      ]
    };
    _bettingRepository.GetBetSlipsAsync(null, Arg.Any<CancellationToken>())
      .Returns(new List<BetSlip> { slip });

    // Act
    var result = await _sut.Handle(new GetBetSlipsQuery(), CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    var summary = result[0];
    summary.Selections.Should().HaveCount(2);
    summary.Selections[0].MatchId.Should().Be(9);
    summary.Selections[0].OutcomeKey.Should().Be("Arsenal");
    summary.Selections[1].OutcomeKey.Should().Be("Chelsea");
    summary.Selections[0].EventTypeName.Should().Be("Match Result (90 min)");
    summary.Selections[1].EventTypeName.Should().Be("Match Result (90 min)");
  }
}
