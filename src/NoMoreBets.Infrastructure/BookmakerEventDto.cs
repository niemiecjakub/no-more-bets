using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure;

public record EventOptionDto(string Label, double Odds)
{
  public static EventOptionDto From(EventOption source) =>
      new(source.Label, source.Odds);
}

/// <summary>API response DTO for a bookmaker event, optionally with mapped <see cref="BettingEventType"/>.</summary>
public record BookmakerEventDto(string? EventTypeName, BettingEventType? EventType, string Title, IReadOnlyList<EventOptionDto> Options)
{
  public static BookmakerEventDto From(BookmakerEvent source)
  {
    BettingEventType? eventType = BookmakerEventTypeMapper.Map(source.Title);
    string? eventTypeName = eventType?.ToString();
    return new(eventTypeName, eventType, source.Title, source.Options.Select(EventOptionDto.From).ToList());
  }
}
