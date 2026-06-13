using MediatR;
using NoMoreBets.Application.AgentSessions.ToolCallDisplay;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

public record GetAgentSessionMessagesQuery(int SessionId) : IRequest<IReadOnlyList<AgentSessionMessageDto>?>;

public sealed class GetAgentSessionMessagesHandler(
  IUnitOfWork unitOfWork,
  AgentToolCallDisplayFormatter displayFormatter)
  : IRequestHandler<GetAgentSessionMessagesQuery, IReadOnlyList<AgentSessionMessageDto>?>
{
  public async Task<IReadOnlyList<AgentSessionMessageDto>?> Handle(
    GetAgentSessionMessagesQuery request,
    CancellationToken cancellationToken)
  {
    if (!await unitOfWork.AgentSessions.SessionExistsAsync(request.SessionId, cancellationToken).ConfigureAwait(false))
      return null;

    var messages = await unitOfWork.AgentSessions
      .GetMessagesAsync(request.SessionId, cancellationToken)
      .ConfigureAwait(false);

    var displayByMessageId = await displayFormatter
      .BuildDisplayByMessageIdAsync(request.SessionId, messages, cancellationToken)
      .ConfigureAwait(false);

    return messages
      .Select(m => new AgentSessionMessageDto(
        m.Id,
        m.SessionId,
        m.Ordinal,
        (int)m.Kind,
        m.Text,
        m.Kind == AgentSessionMessageKind.FunctionCall && displayByMessageId.TryGetValue(m.Id, out var display)
          ? display
          : null))
      .ToList();
  }
}
