using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetBettingPerformanceStats;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting.GetBettingPerformanceStats;

public class GetBettingPerformanceStatsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetBettingPerformanceStatsHandler _sut;

  public GetBettingPerformanceStatsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetBettingPerformanceStatsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_AggregatesSettledSlipsAndIgnoresPending()
  {
    // Two settled slips (one won single, one lost 2-leg parlay) and one pending slip that must be ignored.
    static BetSlip Slip(BetStatus status, decimal stake, decimal odds, decimal probability, params BetStatus[] selectionStatuses) => new()
    {
      StakeAmount = stake,
      TotalOdds = odds,
      PotentialPayout = stake * odds,
      EstimatedWinProbability = probability,
      BetStatus = status,
      Selections = selectionStatuses
        .Select(s => new BetSelection { BetEventType = BettingEventType.MatchResult, OddsAtPlacement = odds, BetStatus = s })
        .ToList()
    };

    _bettingRepository.GetBettingPhaseBetSlipsAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>()).Returns(
    [
      Slip(BetStatus.Won, 100m, 2.0m, 0.6m, BetStatus.Won),
      Slip(BetStatus.Lost, 50m, 3.0m, 0.6m, BetStatus.Won, BetStatus.Lost),
      Slip(BetStatus.Pending, 10m, 1.5m, 0.5m, BetStatus.Pending),
    ]);

    var stats = await _sut.Handle(new GetBettingPerformanceStatsQuery(), CancellationToken.None);

    stats.Overall.SlipCount.Should().Be(2);
    stats.Overall.TotalStaked.Should().Be(150m);
    stats.Overall.TotalReturned.Should().Be(200m);
    stats.Overall.Roi.Should().Be(0.3333m);
    stats.Overall.HitRate.Should().Be(0.5);
    stats.ByParlaySize.Should().HaveCount(2);
    stats.ByMarketType.Should().ContainSingle().Which.SelectionCount.Should().Be(3);
    var calibration = stats.Calibration.Should().ContainSingle().Subject;
    calibration.AverageEstimatedProbability.Should().Be(0.6);
    calibration.ActualWinRate.Should().Be(0.5);
  }
}
