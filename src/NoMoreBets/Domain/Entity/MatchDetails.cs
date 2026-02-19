using System.Text.Json;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

namespace NoMoreBets.Domain.Entity;

public class MatchDetails
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int Id { get; set; }
  public string FotmobUrl { get; set; } = null!;
  public string FotmobDetailsJson { get; set; } = null!;

  public int? MatchId { get; set; }
  public Match? Match { get; set; }

  public MatchDetailsDto? GetFotmobDetails() =>
    string.IsNullOrEmpty(FotmobDetailsJson)
      ? null
      : JsonSerializer.Deserialize<MatchDetailsDto>(FotmobDetailsJson, JsonOptions);
}
