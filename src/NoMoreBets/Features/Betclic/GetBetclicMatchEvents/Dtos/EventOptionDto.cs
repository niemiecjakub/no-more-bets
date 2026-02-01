using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;

/// <summary>API response DTO for a single betting option.</summary>
public record EventOptionDto(string Label, double Odds)
{
    public static EventOptionDto From(EventOption source) =>
        new(source.Label, source.Odds);
}
