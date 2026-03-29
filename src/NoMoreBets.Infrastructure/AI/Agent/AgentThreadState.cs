namespace NoMoreBets.Infrastructure.AI.Agent;

/// <summary>
/// Process-wide holder for the current OpenAI Assistants thread id so conversation can resume across scoped <see cref="Runner"/> instances.
/// </summary>
public sealed class AgentThreadState
{
  private readonly object _lock = new();
  private string? _threadId;

  public string? ThreadId
  {
    get
    {
      lock (_lock)
      {
        return _threadId;
      }
    }
    set
    {
      lock (_lock)
      {
        _threadId = value;
      }
    }
  }
}
