using FluentAssertions;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Application.Tests.AgentSessions;

public class AgentSessionPhaseExtensionsTests
{
  [Theory]
  [InlineData(AgentSessionPhase.Research, true)]
  [InlineData(AgentSessionPhase.DailySlip, true)]
  [InlineData(AgentSessionPhase.Betting, false)]
  [InlineData(AgentSessionPhase.Reflection, false)]
  [InlineData(AgentSessionPhase.InternetResearch, false)]
  [InlineData(AgentSessionPhase.MemoryCleanup, false)]
  public void IsPaperSlipPhase_ReturnsExpected(AgentSessionPhase phase, bool expected)
  {
    // Act
    var result = phase.IsPaperSlipPhase();

    // Assert
    result.Should().Be(expected);
  }
}
