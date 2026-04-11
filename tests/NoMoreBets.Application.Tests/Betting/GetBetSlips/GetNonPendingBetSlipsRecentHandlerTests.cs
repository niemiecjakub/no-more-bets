using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetBetSlips;

public class GetNonPendingBetSlipsRecentHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetNonPendingBetSlipsRecentHandler _sut;

  public GetNonPendingBetSlipsRecentHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetNonPendingBetSlipsRecentHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_PassesLastDaysToRepository()
  {
    _bettingRepository.GetNonPendingBetSlipsCreatedInLastDaysAsync(14, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());

    await _sut.Handle(new GetNonPendingBetSlipsRecentQuery(14), CancellationToken.None);

    await _bettingRepository.Received(1).GetNonPendingBetSlipsCreatedInLastDaysAsync(14, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_OrdersSelectionsByIdAscending_AndMapsDisplayNames()
  {
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
      BetStatus = BetStatus.Won,
      Selections =
      [
        new BetSelection
        {
          Id = 2,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Away,
          OddsAtPlacement = 2.5m,
          BetStatus = BetStatus.Won,
          Match = match
        },
        new BetSelection
        {
          Id = 1,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Home,
          OddsAtPlacement = 1.9m,
          BetStatus = BetStatus.Won,
          Match = match
        }
      ]
    };
    _bettingRepository.GetNonPendingBetSlipsCreatedInLastDaysAsync(7, Arg.Any<CancellationToken>())
      .Returns(new List<BetSlip> { slip });

    var result = await _sut.Handle(new GetNonPendingBetSlipsRecentQuery(7), CancellationToken.None);

    result.Should().ContainSingle();
    var summary = result[0];
    summary.Status.Should().Be(BetStatus.Won);
    summary.Selections.Should().HaveCount(2);
    summary.Selections[0].MatchId.Should().Be(9);
    summary.Selections[0].OutcomeKey.Should().Be("Arsenal");
    summary.Selections[1].OutcomeKey.Should().Be("Chelsea");
  }
}
