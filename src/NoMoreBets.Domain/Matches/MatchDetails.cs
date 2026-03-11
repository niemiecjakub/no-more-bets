using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public class MatchDetails
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int Id { get; set; }
  public string? FotmobUrl { get; set; }
  public string? FotmobDetailsJson { get; set; }
  public string? FotmobReview { get; set; }

  public int? MatchId { get; set; }
  public Match? Match { get; set; }

  public FotmobDetailsPayload? GetFotmobDetails() =>
    string.IsNullOrEmpty(FotmobDetailsJson)
      ? null
      : JsonSerializer.Deserialize<FotmobDetailsPayload>(FotmobDetailsJson, JsonOptions);
}
