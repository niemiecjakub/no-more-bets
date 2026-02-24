using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;

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
