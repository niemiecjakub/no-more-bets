using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Domain.AgentSessions;

public class AgentSessionReflectionBetSlip
{
  public int AgentSessionId { get; set; }
  public int BetSlipId { get; set; }

  public AgentSession AgentSession { get; set; } = null!;
  public BetSlip BetSlip { get; set; } = null!;
}
