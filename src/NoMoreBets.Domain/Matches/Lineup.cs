using System.Text.Json;
using NoMoreBets.Features.Rotowire.Model;

namespace NoMoreBets.Domain.Matches;

public class Lineup
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int MatchId { get; set; }
  public string HomeTeamJson { get; set; } = null!;
  public string AwayTeamJson { get; set; } = null!;
  public DateTime UpdatedAt { get; set; }

  public Match Match { get; set; } = null!;

  public TeamLineup GetHomeTeamLineup() => JsonSerializer.Deserialize<TeamLineup>(HomeTeamJson, JsonOptions)!;

  public TeamLineup GetAwayTeamLineup() => JsonSerializer.Deserialize<TeamLineup>(AwayTeamJson, JsonOptions)!;
}
