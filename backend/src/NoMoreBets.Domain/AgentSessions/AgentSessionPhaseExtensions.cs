namespace NoMoreBets.Domain.AgentSessions;

public static class AgentSessionPhaseExtensions
{
  public static bool IsPaperSlipPhase(this AgentSessionPhase phase) =>
    phase is AgentSessionPhase.Research or AgentSessionPhase.DailySlip;
}
