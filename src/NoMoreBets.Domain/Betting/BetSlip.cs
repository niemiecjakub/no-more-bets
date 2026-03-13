using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public class BetSlip
{
  public int Id { get; set; }
  public decimal StakeAmount { get; set; }
  public decimal TotalOdds { get; set; }
  public decimal PotentialPayout { get; set; }
  public int StatusId { get; set; }
  public DateTime CreatedAt { get; set; }

  public BetStatusEntity BetStatusEntity { get; set; } = null!;
  public ICollection<BetSelection> Selections { get; set; } = new List<BetSelection>();

  [NotMapped]
  public BetStatus BetStatus
  {
    get => (BetStatus)StatusId;
    set => StatusId = (int)value;
  }
}
