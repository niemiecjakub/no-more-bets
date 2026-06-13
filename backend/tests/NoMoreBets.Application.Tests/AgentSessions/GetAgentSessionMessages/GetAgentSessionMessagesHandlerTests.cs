using FluentAssertions;
using NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;
using NoMoreBets.Application.AgentSessions.ToolCallDisplay;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using DomainClub = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;
using NSubstitute;

namespace NoMoreBets.Application.Tests.AgentSessions.GetAgentSessionMessages;

public class GetAgentSessionMessagesHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IAgentSessionRepository _agentSessions = Substitute.For<IAgentSessionRepository>();
  private readonly AgentToolCallDisplayFormatter _displayFormatter;
  private readonly GetAgentSessionMessagesHandler _sut;

  public GetAgentSessionMessagesHandlerTests()
  {
    _unitOfWork.AgentSessions.Returns(_agentSessions);
    _displayFormatter = new AgentToolCallDisplayFormatter(_unitOfWork);
    _sut = new GetAgentSessionMessagesHandler(_unitOfWork, _displayFormatter);
  }

  [Fact]
  public async Task Handle_WhenSessionMissing_ReturnsNull()
  {
    // Arrange
    _agentSessions.SessionExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

    // Act
    var result = await _sut.Handle(new GetAgentSessionMessagesQuery(1), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_AttachesToolCallDisplayOnlyForFunctionCalls()
  {
    // Arrange
    const int sessionId = 5;
    const int matchId = 99;
    var messages = new List<AgentSessionMessage>
    {
      new() { Id = 1, SessionId = sessionId, Ordinal = 0, Kind = AgentSessionMessageKind.Message, Text = "Hello" },
      new()
      {
        Id = 2,
        SessionId = sessionId,
        Ordinal = 1,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = $$"""{"name":"betting_getCurrentOdds","arguments":[{"name":"matchId","value":"{{matchId}}"}]}""",
      },
    };

    var betting = Substitute.For<IBettingRepository>();
    var matches = Substitute.For<IMatchRepository>();
    _unitOfWork.Betting.Returns(betting);
    _unitOfWork.Matches.Returns(matches);

    _agentSessions.SessionExistsAsync(sessionId, Arg.Any<CancellationToken>()).Returns(true);
    _agentSessions.GetMessagesAsync(sessionId, Arg.Any<CancellationToken>()).Returns(messages);
    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns([
        new Match
        {
          Id = matchId,
          HomeClubId = 1,
          AwayClubId = 2,
          HomeClub = new DomainClub { Id = 1, Name = "Arsenal" },
          AwayClub = new DomainClub { Id = 2, Name = "Chelsea" },
        },
      ]);

    // Act
    var result = await _sut.Handle(new GetAgentSessionMessagesQuery(sessionId), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result![0].ToolCallDisplay.Should().BeNull();
    result[1].ToolCallDisplay.Should().NotBeNull();
    result[1].ToolCallDisplay!.Label.Should().Be("Check current odds");
    result[1].ToolCallDisplay!.Details.Should().ContainSingle("Arsenal vs Chelsea");
  }
}
