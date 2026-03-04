namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Represents country information from SoccerData API.</summary>
public record CountryInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
