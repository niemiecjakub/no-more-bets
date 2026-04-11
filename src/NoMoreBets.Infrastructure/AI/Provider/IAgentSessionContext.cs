namespace NoMoreBets.Infrastructure.AI.Provider;

/// <summary>
/// Scoped per agent run; plugins read <see cref="SessionId"/> when persisting outcomes linked to the transcript.
/// </summary>
public interface IAgentSessionContext
{
  int? SessionId { get; set; }
}
