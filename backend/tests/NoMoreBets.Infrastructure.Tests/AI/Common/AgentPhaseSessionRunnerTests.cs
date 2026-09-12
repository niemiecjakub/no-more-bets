using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.Tests.AI.Common;

public class AgentPhaseSessionRunnerTests
{
  [Fact]
  public async Task RunAsync_WhenNoMessages_DeletesSession()
  {
    var sessions = Substitute.For<IAgentSessionRepository>();
    sessions.CreateSessionAsync(Arg.Any<AgentSessionPhase>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
      .Returns(42);

    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.DailySlip,
      "test",
      agentBuilder: null!,
      new AgentRunMessageCollector(),
      sessions,
      new AgentSessionContext(),
      Substitute.For<IServiceProvider>(),
      NullLogger.Instance,
      (_, _, _) => Task.CompletedTask,
      CancellationToken.None);

    result.TranscriptPersisted.Should().BeFalse();
    result.Messages.Should().BeEmpty();
    await sessions.Received(1).DeleteSessionAsync(42, Arg.Any<CancellationToken>());
    await sessions.DidNotReceive().AddMessagesAsync(
      Arg.Any<int>(),
      Arg.Any<IReadOnlyList<AgentSessionMessage>>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RunAsync_WhenMessagesPresent_PersistsTranscript()
  {
    var sessions = Substitute.For<IAgentSessionRepository>();
    sessions.CreateSessionAsync(Arg.Any<AgentSessionPhase>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
      .Returns(7);

    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.MemoryCleanup,
      "test",
      agentBuilder: null!,
      new AgentRunMessageCollector(),
      sessions,
      new AgentSessionContext(),
      Substitute.For<IServiceProvider>(),
      NullLogger.Instance,
      (_, messages, _) =>
      {
        messages.Add(new Message("hello"));
        return Task.CompletedTask;
      },
      CancellationToken.None);

    result.TranscriptPersisted.Should().BeTrue();
    result.Messages.Should().ContainSingle();
    await sessions.Received(1).AddMessagesAsync(
      7,
      Arg.Any<IReadOnlyList<AgentSessionMessage>>(),
      Arg.Any<CancellationToken>());
    await sessions.DidNotReceive().DeleteSessionAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
  }
}
