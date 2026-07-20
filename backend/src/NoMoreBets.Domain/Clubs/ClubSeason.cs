using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Domain.Clubs;

public class ClubSeason
{
  public int ClubId { get; set; }
  public int SeasonId { get; set; }

  public Club Club { get; set; } = null!;
  public Season Season { get; set; } = null!;
}
