using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchEvents;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Players;

namespace NoMoreBets.Application.Tests.Matches.GetMatchEvents;

public class GetMatchEventsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchEventsHandler _sut;

  public GetMatchEventsHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchEventsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoEvents_ReturnsEmptyList()
  {
    _matches
      .GetMatchEventsForMatchAsync(5, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<MatchEvent>());

    var result = await _sut.Handle(new GetMatchEventsQuery(5), CancellationToken.None);

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_OrdersEventsByMinuteAscending()
  {
    var events = new[]
    {
      CreateMatchEvent(minute: 67, id: 3),
      CreateMatchEvent(minute: 12, id: 1),
      CreateMatchEvent(minute: 45, id: 2),
    };
    _matches
      .GetMatchEventsForMatchAsync(1, Arg.Any<CancellationToken>())
      .Returns(events);

    var result = await _sut.Handle(new GetMatchEventsQuery(1), CancellationToken.None);

    result.Select(e => e.Minute).Should().Equal(12, 45, 67);
  }

  [Fact]
  public async Task Handle_WhenEventsExist_MapsFields()
  {
    var matchEvent = new MatchEvent
    {
      Id = 10,
      MatchId = 3,
      ClubId = 17,
      EventTypeId = (int)MatchEventType.Goal,
      Minute = 23,
      Player = new Player { Id = 1, Name = "Erling Haaland", SoccerdataId = 99 },
      EventTypeEntity = new MatchEventTypeEntity { Id = (int)MatchEventType.Goal, Name = nameof(MatchEventType.Goal) },
    };
    _matches
      .GetMatchEventsForMatchAsync(3, Arg.Any<CancellationToken>())
      .Returns(new[] { matchEvent });

    var result = await _sut.Handle(new GetMatchEventsQuery(3), CancellationToken.None);

    result.Should().ContainSingle()
      .Which.Should().BeEquivalentTo(new MatchEventDto(
        "Erling Haaland",
        17,
        (int)MatchEventType.Goal,
        nameof(MatchEventType.Goal),
        23));
  }

  private static MatchEvent CreateMatchEvent(int minute, int id) =>
    new()
    {
      Id = id,
      MatchId = 1,
      ClubId = 17,
      EventTypeId = (int)MatchEventType.Goal,
      Minute = minute,
      Player = new Player { Id = 1, Name = "Player", SoccerdataId = 1 },
      EventTypeEntity = new MatchEventTypeEntity { Id = (int)MatchEventType.Goal, Name = nameof(MatchEventType.Goal) },
    };
}
