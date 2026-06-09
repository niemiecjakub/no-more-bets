using NoMoreBets.Application.Common.Dto;

namespace NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

public sealed class AgentRunMessageCollector
{
  private List<IMessage>? _messages;

  public void SetMessages(IReadOnlyList<IMessage> messages) => _messages = messages.ToList();

  public List<IMessage> TakeMessages()
  {
    var result = _messages ?? [];
    _messages = null;
    return result;
  }
}
