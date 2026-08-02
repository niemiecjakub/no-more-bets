using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchResearchBetSlip;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetMatchResearchBetSlip;

public class GetMatchResearchBetSlipHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetMatchResearchBetSlipHandler _sut;

  public GetMatchResearchBetSlipHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetMatchResearchBetSlipHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoSlip_ReturnsNull()
  {
    // Arrange
    _bettingRepository.GetLatestResearchBetSlipForMatchAsync(1, Arg.Any<CancellationToken>())
      .Returns((BetSlip?)null);

    // Act
    var result = await _sut.Handle(new GetMatchResearchBetSlipQuery(1), CancellationToken.None);

    // Assert
    result.Should().BeNull();
    await _bettingRepository.Received(1).GetLatestResearchBetSlipForMatchAsync(1, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSlipExists_ReturnsSummary()
  {
    // Arrange
    var match = new Match
    {
      Id = 5,
      HomeClub = new ClubEntity { Name = "A" },
      AwayClub = new ClubEntity { Name = "B" }
    };
    var slip = new BetSlip
    {
      Id = 10,
      CreatedAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      BetStatus = BetStatus.Pending,
      Selections =
      [
        new BetSelection
        {
          Id = 1,
          MatchId = 5,
          BetEventType = BettingEventType.BothTeamsToScore,
          BetEventOption = BettingEventOption.BothTeamsToScore_Yes,
          OddsAtPlacement = 2m,
          BetStatus = BetStatus.Pending,
          Match = match
        }
      ]
    };
    _bettingRepository.GetLatestResearchBetSlipForMatchAsync(5, Arg.Any<CancellationToken>())
      .Returns(slip);

    // Act
    var result = await _sut.Handle(new GetMatchResearchBetSlipQuery(5), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(10);
    result.Selections.Should().ContainSingle();
    result.Selections[0].MatchId.Should().Be(5);
  }
}
