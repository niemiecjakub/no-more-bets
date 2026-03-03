using NoMoreBets.Domain.Leagues.Dto;

namespace NoMoreBets.Domain.Clubs.Dto;
public record XgStatsDto(
    int Position,
    string? PositionChange,
    int TeamId,
    string TeamName,
    string TeamShortname,
    string TeamLogoUrl,
    double Xg,
    string? XgDiff,
    double Xga,
    string? XgaDiff,
    double Xpts,
    string? XptsDiff)
{
  public static XgStatsDto From(XgStats source) =>
      new(
          source.Position,
          source.PositionChange,
          source.TeamId,
          source.TeamName,
          source.TeamShortname,
          source.TeamLogoUrl,
          source.Xg,
          source.XgDiff,
          source.Xga,
          source.XgaDiff,
          source.Xpts,
          source.XptsDiff);
}