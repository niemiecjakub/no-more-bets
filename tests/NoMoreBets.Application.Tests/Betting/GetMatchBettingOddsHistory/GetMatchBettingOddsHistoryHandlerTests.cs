using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetMatchBettingOddsHistory;

public class GetMatchBettingOddsHistoryHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly GetMatchBettingOddsHistoryHandler _sut;

  public GetMatchBettingOddsHistoryHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _unitOfWork.Matches.Returns(_matchRepository);
    _matchRepository.GetMatchByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<Match?>(null));
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

  [Fact]
  public async Task Handle_WhenNoSnapshots_ReturnsNull()
  {
    // Arrange
    _bettingRepository.GetBettingOddsSnapshotsForMatchAsync(99, Arg.Any<CancellationToken>())
      .Returns(new List<BettingOddsSnapshot>());

    // Act
    var result = await _sut.Handle(new GetMatchBettingOddsHistoryQuery(99), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WithMatch_UsesDisplayNamesForMarketAndOutcome()
  {
    // Arrange
    var t1 = DateTime.UtcNow.AddHours(-1);
    var snapshot = BuildSnapshot(t1, 2.0);
    _bettingRepository.GetBettingOddsSnapshotsForMatchAsync(10, Arg.Any<CancellationToken>())
      .Returns(new List<BettingOddsSnapshot> { snapshot });
    var match = new Match
    {
      Id = 10,
      HomeClub = new NoMoreBets.Domain.Clubs.Club { Name = "Arsenal" },
      AwayClub = new NoMoreBets.Domain.Clubs.Club { Name = "Chelsea" },
    };
    _matchRepository.GetMatchByIdAsync(10, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<Match?>(match));

    // Act
    var result = await _sut.Handle(new GetMatchBettingOddsHistoryQuery(10), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var market = result![0];
    market.MarketKey.Should().Be(nameof(BettingEventType.MatchResult));
    market.MarketDisplayName.Should().Be("Match Result (90 min)");
    market.Outcomes.Should().ContainSingle();
    market.Outcomes[0].OutcomeName.Should().Be("Arsenal");
  }

  [Fact]
  public async Task Handle_WhenOptionLabelDoesNotParse_KeepsRawLabel()
  {
    // Arrange
    var t1 = DateTime.UtcNow.AddHours(-1);
    var row = new BettingOddsSnapshotRow
    {
      EventTypeId = (int)BettingEventType.MatchResult,
      EventTypeEntity = new BettingEventTypeEntity { Id = (int)BettingEventType.MatchResult, Name = nameof(BettingEventType.MatchResult) },
      EventOptionId = null,
      EventOptionEntity = new BettingEventOptionEntity { Id = 0, Name = "UnknownFutureOption" },
      Odds = 1.5m,
    };
    var snapshot = new BettingOddsSnapshot { SnapshotTime = t1, MatchId = 10 };
    snapshot.Rows.Add(row);
    _bettingRepository.GetBettingOddsSnapshotsForMatchAsync(10, Arg.Any<CancellationToken>())
      .Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.Handle(new GetMatchBettingOddsHistoryQuery(10), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result![0].Outcomes[0].OutcomeName.Should().Be("UnknownFutureOption");
  }

  [Fact]
  public async Task Handle_OrdersMarketsByConfiguredDisplayOrder()
  {
    // Arrange — rows appear in snapshot in an order that does not match UI order
    var t1 = DateTime.UtcNow.AddHours(-1);
    var snapshot = new BettingOddsSnapshot { SnapshotTime = t1, MatchId = 10 };
    snapshot.Rows.Add(BuildRow(BettingEventType.ExactScore, BettingEventOption.CorrectScore_0_0));
    snapshot.Rows.Add(BuildRow(BettingEventType.OverUnderGoals, BettingEventOption.TotalGoals_Over_2_5));
    snapshot.Rows.Add(BuildRow(BettingEventType.MatchResult, BettingEventOption.MatchResult_Home));
    snapshot.Rows.Add(BuildRow(BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes));
    _bettingRepository.GetBettingOddsSnapshotsForMatchAsync(10, Arg.Any<CancellationToken>())
      .Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.Handle(new GetMatchBettingOddsHistoryQuery(10), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Select(m => m.MarketKey).Should().ContainInOrder(
      nameof(BettingEventType.MatchResult),
      nameof(BettingEventType.BothTeamsToScore),
      nameof(BettingEventType.OverUnderGoals),
      nameof(BettingEventType.ExactScore));
  }

  private static BettingOddsSnapshotRow BuildRow(BettingEventType eventType, BettingEventOption option)
  {
    var optionName = option.ToString();
    return new BettingOddsSnapshotRow
    {
      EventTypeId = (int)eventType,
      EventTypeEntity = new BettingEventTypeEntity { Id = (int)eventType, Name = eventType.ToString() },
      EventOptionId = (int)option,
      EventOptionEntity = new BettingEventOptionEntity { Id = (int)option, Name = optionName },
      Odds = 2.0m,
    };
  }

  private static BettingOddsSnapshot BuildSnapshot(DateTime snapshotTime, double homeOdds)
  {
    const string homeOutcome = "MatchResult_Home";
    var row = new BettingOddsSnapshotRow
    {
      EventTypeId = (int)BettingEventType.MatchResult,
      EventTypeEntity = new BettingEventTypeEntity { Id = (int)BettingEventType.MatchResult, Name = nameof(BettingEventType.MatchResult) },
      EventOptionId = (int)BettingEventOption.MatchResult_Home,
      EventOptionEntity = new BettingEventOptionEntity { Id = (int)BettingEventOption.MatchResult_Home, Name = homeOutcome },
      Odds = (decimal)homeOdds
    };

    var snapshot = new BettingOddsSnapshot { SnapshotTime = snapshotTime, MatchId = 10 };
    snapshot.Rows.Add(row);
    return snapshot;
  }
}
