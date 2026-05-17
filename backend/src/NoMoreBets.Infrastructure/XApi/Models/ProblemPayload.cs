namespace NoMoreBets.Infrastructure.XApi.Models;

internal sealed class ProblemPayload
{
  public string? Type { get; set; }

  public string? Title { get; set; }

  public string? Detail { get; set; }

  public int? Status { get; set; }
}
