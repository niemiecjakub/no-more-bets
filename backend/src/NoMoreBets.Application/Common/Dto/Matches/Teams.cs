using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Represents home and away teams from SoccerData API.</summary>
public record Teams
{
    public TeamInfo Home { get; init; } = null!;
    public TeamInfo Away { get; init; } = null!;
}
