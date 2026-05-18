using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

public record GetAgentSessionMessagesQuery(int SessionId) : IRequest<IReadOnlyList<AgentSessionMessageDto>?>;

public sealed class GetAgentSessionMessagesHandler(IUnitOfWork unitOfWork)
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

    return messages
      .Select(m => new AgentSessionMessageDto(m.Id, m.SessionId, m.Ordinal, (int)m.Kind, m.Text))
      .ToList();
  }
}
