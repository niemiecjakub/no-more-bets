namespace NoMoreBets.Infrastructure.AI.Common;

/// <summary>
/// Scoped per agent run; plugins read <see cref="SessionId"/> when persisting outcomes linked to the transcript.
/// </summary>
public sealed class AgentSessionContext
{
  public int? SessionId { get; set; }
}
