namespace NoMoreBets.Domain.Clubs;

public class ClubDailySummary
{
  public int Id { get; set; }
  public int ClubId { get; set; }
  public DateOnly Date { get; set; }
  public string Summary { get; set; } = null!;

  public Club Club { get; set; } = null!;

  public override string ToString()
  {
    return $"[{Date}] {Summary}";
  }
}
