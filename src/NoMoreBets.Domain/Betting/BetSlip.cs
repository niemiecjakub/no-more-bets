using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public class BetSlip
{
  public int Id { get; set; }
  public int? AgentSessionId { get; set; }
  public decimal StakeAmount { get; set; }
  public decimal TotalOdds { get; set; }
  public decimal PotentialPayout { get; set; }
  public int StatusId { get; set; }
  public DateTime CreatedAt { get; set; }

  public AgentSession? AgentSession { get; set; }
  public BetStatusEntity BetStatusEntity { get; set; } = null!;
  public ICollection<BetSelection> Selections { get; set; } = new List<BetSelection>();
  public ICollection<Bankroll> Bankrolls { get; set; } = new List<Bankroll>();

  [NotMapped]
  public BetStatus BetStatus
  {
    get => (BetStatus)StatusId;
    set => StatusId = (int)value;
  }

  /// <summary>
  /// Parlay rollup: any leg <see cref="BetStatus.Lost"/> loses the slip; all <see cref="BetStatus.Won"/> wins; otherwise pending.
  /// </summary>
  public BetStatus ComputeStatusFromSelections()
  {
    var list = Selections as IReadOnlyCollection<BetSelection> ?? Selections.ToList();
    if (list.Count == 0)
    {
      return BetStatus.Pending;
    }

    if (list.Any(s => s.BetStatus == BetStatus.Lost))
    {
      return BetStatus.Lost;
    }

    if (list.All(s => s.BetStatus == BetStatus.Won))
    {
      return BetStatus.Won;
    }

    return BetStatus.Pending;
  }
}
