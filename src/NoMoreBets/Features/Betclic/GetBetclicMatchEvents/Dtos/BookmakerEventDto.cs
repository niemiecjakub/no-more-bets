using System.Linq;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;

/// <summary>API response DTO for a bookmaker event.</summary>
public record BookmakerEventDto(string Title, IReadOnlyList<EventOptionDto> Options)
{
    public static BookmakerEventDto From(BookmakerEvent source) =>
        new(source.Title, source.Options.Select(EventOptionDto.From).ToList());
}
