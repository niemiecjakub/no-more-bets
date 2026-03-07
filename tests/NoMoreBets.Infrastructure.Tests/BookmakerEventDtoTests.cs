using FluentAssertions;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure;

namespace NoMoreBets.Infrastructure.Tests;

public class BookmakerEventDtoTests
{
  [Fact]
  public void From_WhenSourceHasTitleAndOptions_MapsToEventTypeAndOptions()
  {
    var source = new BookmakerEvent
    {
      Title = "Gole Powyżej/Poniżej",
      Options =
      [
        new EventOption { Label = "Powyżej 2.5", Odds = 1.85 },
        new EventOption { Label = "Poniżej 2.5", Odds = 2.00 }
      ]
    };

    var result = BookmakerEventDto.From(source);

    result.Title.Should().Be("Gole Powyżej/Poniżej");
    result.EventType.Should().Be(BettingEventType.OverUnderGoals);
    result.EventTypeName.Should().Be(nameof(BettingEventType.OverUnderGoals));
    result.Options.Should().HaveCount(2);
    result.Options[0].Label.Should().Be("Powyżej 2.5");
    result.Options[0].Odds.Should().Be(1.85);
    result.Options[1].Label.Should().Be("Poniżej 2.5");
    result.Options[1].Odds.Should().Be(2.00);
  }

  [Fact]
  public void From_WhenSourceTitleIsUnknown_EventTypeIsNull()
  {
    var source = new BookmakerEvent
    {
      Title = "Unknown Market Title",
      Options = [new EventOption { Label = "Yes", Odds = 1.5 }]
    };

    var result = BookmakerEventDto.From(source);

    result.Title.Should().Be("Unknown Market Title");
    result.EventType.Should().BeNull();
    result.EventTypeName.Should().BeNull();
    result.Options.Should().HaveCount(1);
    result.Options[0].Label.Should().Be("Yes");
    result.Options[0].Odds.Should().Be(1.5);
  }
}
