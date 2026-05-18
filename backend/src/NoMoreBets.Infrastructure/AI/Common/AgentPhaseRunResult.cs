using NoMoreBets.Application.Common.Dto;

namespace NoMoreBets.Infrastructure.AI.Common;

public sealed record AgentPhaseRunResult(IReadOnlyList<IMessage> Messages, int? SessionId);
