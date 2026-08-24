using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.AgentSessions;

public class AgentSession
{
  public int Id { get; set; }
  public AgentSessionPhase Phase { get; set; }
  public DateTime StartedAt { get; set; }

  public ICollection<AgentSessionMessage> Messages { get; set; } = new List<AgentSessionMessage>();
  public ICollection<MatchAnalysis> MatchAnalyses { get; set; } = new List<MatchAnalysis>();
  public ICollection<BetSlip> BetSlips { get; set; } = new List<BetSlip>();
  public ICollection<BetSlip> ReflectedBetSlips { get; set; } = new List<BetSlip>();
}
