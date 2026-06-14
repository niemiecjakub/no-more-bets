namespace NoMoreBets.Domain.AgentSessions;

public class AgentSessionMessage
{
  public int Id { get; set; }
  public int SessionId { get; set; }
  public int Ordinal { get; set; }
  public AgentSessionMessageKind Kind { get; set; }
  public string Text { get; set; } = null!;
  public string? Metadata { get; set; }

  public AgentSession Session { get; set; } = null!;
}
