using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting.GetMatchBettingOddsHistory;

public class GetMatchBettingOddsHistoryHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetMatchBettingOddsHistoryHandler _sut;

  public GetMatchBettingOddsHistoryHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetMatchBettingOddsHistoryHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenOddsRepeatAcrossSnapshots_CollapsesToChangedSegmentsOnly()
  {
    // Arrange
    var t1 = DateTime.UtcNow.AddHours(-3);
    var t2 = DateTime.UtcNow.AddHours(-2);
    var t3 = DateTime.UtcNow.AddHours(-1);
    var snapshot1 = BuildSnapshot(t1, 1.90);
    var snapshot2 = BuildSnapshot(t2, 1.90);
    var snapshot3 = BuildSnapshot(t3, 2.05);
    _bettingRepository.GetBettingOddsSnapshotsForMatchAsync(10, Arg.Any<CancellationToken>())
      .Returns(new List<BettingOddsSnapshot> { snapshot1, snapshot2, snapshot3 });

    // Act
    var result = await _sut.Handle(new GetMatchBettingOddsHistoryQuery(10), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var timeline = result![0].Outcomes[0].Timeline;
    timeline.Should().HaveCount(2);
    timeline[0].Price.Should().Be(1.90);
    timeline[1].Price.Should().Be(2.05);
  }

  private static BettingOddsSnapshot BuildSnapshot(DateTime snapshotTime, double homeOdds)
  {
    const string homeOutcome = "MatchResult_Home";
    var row = new BettingOddsSnapshotRow
    {
      EventTypeId = (int)BettingEventType.MatchResult,
      EventTypeEntity = new BettingEventTypeEntity { Id = (int)BettingEventType.MatchResult, Name = "Match Result" },
      EventOptionId = (int)BettingEventOption.MatchResult_Home,
      EventOptionEntity = new BettingEventOptionEntity { Id = (int)BettingEventOption.MatchResult_Home, Name = homeOutcome },
      Odds = (decimal)homeOdds
    };

    var snapshot = new BettingOddsSnapshot { SnapshotTime = snapshotTime, MatchId = 10 };
    snapshot.Rows.Add(row);
    return snapshot;
  }
}
