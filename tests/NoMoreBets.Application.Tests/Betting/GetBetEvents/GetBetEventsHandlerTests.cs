using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting;
using NoMoreBets.Application.Betting.GetBetEvents;
using NoMoreBets.Application.Common.Dto.Betting;

namespace NoMoreBets.Application.Tests.Betting.GetBetEvents;

public class GetBetEventsHandlerTests
{
  private readonly IBetEventsProvider _provider = Substitute.For<IBetEventsProvider>();
  private readonly GetBetEventsHandler _sut;

  public GetBetEventsHandlerTests()
  {
    _sut = new GetBetEventsHandler(_provider);
  }

  [Fact]
  public async Task Handle_DelegatesToProvider_WithUrlAndExpand()
  {
    // Arrange
    var expected = new List<BookmakerEvent>
    {
      new() { Title = "Market", Options = [] }
    };
    _provider.GetMatchEventsAsync("https://game", true, Arg.Any<CancellationToken>())
      .Returns(expected);

    // Act
    var result = await _sut.Handle(new GetBetclicMatchEventsQuery("https://game", Expand: true), CancellationToken.None);

    // Assert
    result.Should().BeSameAs(expected);
    await _provider.Received(1).GetMatchEventsAsync("https://game", true, Arg.Any<CancellationToken>());
  }
}
