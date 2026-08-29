using FluentAssertions;
using NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;
using NoMoreBets.Application.AgentSessions.ToolCallDisplay;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using DomainClub = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;
using NSubstitute;

namespace NoMoreBets.Application.Tests.AgentSessions.ToolCallDisplay;

public class AgentToolCallDisplayFormatterTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IAgentSessionRepository _agentSessions = Substitute.For<IAgentSessionRepository>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly AgentToolCallDisplayFormatter _sut;

  public AgentToolCallDisplayFormatterTests()
  {
    _unitOfWork.AgentSessions.Returns(_agentSessions);
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.Matches.Returns(_matches);
    _sut = new AgentToolCallDisplayFormatter(_unitOfWork);
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_WithMatchId_ResolvesTeamNames()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 10;
    const int matchId = 2776;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = $$"""{"name":"betting_getCurrentOdds","arguments":[{"name":"matchId","value":"{{matchId}}"}]}""",
      },
    };

    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns([CreateMatch(matchId, "Arsenal", "Chelsea")]);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be("Check current odds");
    result[messageId].Category.Should().Be("betting");
    result[messageId].Details.Should().ContainSingle("Arsenal vs Chelsea");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_WithMarket_ShowsHumanizedMarket()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 12;
    const int matchId = 2776;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = $$"""{"name":"betting_getCurrentOddsForMarket","arguments":[{"name":"matchId","value":"{{matchId}}"},{"name":"market","value":"BothTeamsToScore"}]}""",
      },
    };

    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns([CreateMatch(matchId, "Arsenal", "Chelsea")]);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be("Check market odds");
    result[messageId].Details.Should().Equal("Arsenal vs Chelsea", "Both Teams To Score");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_WithoutClubId_DoesNotShowClubZero()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 11;
    const int matchId = 100;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = $$"""{"name":"match_getLeagueTable","arguments":[{"name":"matchId","value":"{{matchId}}"}]}""",
      },
    };

    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns([CreateMatch(matchId, "Home FC", "Away FC")]);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Details.Should().NotContain("Club #0");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_UsesSessionMatch_WhenToolHasNoArguments()
  {
    // Arrange
    const int sessionId = 2;
    const int messageId = 12;
    const int matchId = 55;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"researchbet_getMatchBasicInfo","arguments":[]}""",
      },
    };

    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int> { [sessionId] = matchId });
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns([CreateMatch(matchId, "Liverpool", "Everton")]);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be(AgentToolCatalog.ResearchBet.GetMatchBasicInfo.DisplayName);
    result[messageId].Category.Should().Be("researchbet");
    result[messageId].Details.Should().ContainSingle("Liverpool vs Everton");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_UnknownTool_FallsBackToRawName()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 13;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"custom_unknownTool","arguments":[{"name":"query","value":"\"hello\""}]}""",
      },
    };

    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<Match>());

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be("custom_unknownTool");
    result[messageId].Category.Should().Be("unknown");
    result[messageId].Details.Should().ContainSingle("hello");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_SearchNews_IncludesMetadataSources()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 20;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"websearch_searchNews","arguments":[{"name":"query","value":"\"Arsenal injury news\""}]}""",
        Metadata = """
          {
            "sources": [
              {
                "title": "Premier League injury update",
                "url": "https://bbc.co.uk/sport/injury",
                "hostname": "bbc.co.uk"
              },
              {
                "title": "Arteta confirms Saka doubt",
                "url": "https://theguardian.com/football/saka",
                "hostname": "theguardian.com"
              }
            ]
          }
          """,
      },
    };

    SetupEmptyContext(sessionId);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be(AgentToolCatalog.WebSearch.SearchNews.DisplayName);
    result[messageId].Category.Should().Be("websearch");
    result[messageId].Details.Should().ContainSingle("Arsenal injury news");
    var webSearchMetadata = result[messageId].Metadata.Should().ContainSingle().Subject
      .Should().BeOfType<WebSearchSourcesToolCallMetadataDto>().Subject;
    webSearchMetadata.Sources.Should().HaveCount(2);
    webSearchMetadata.Sources[0].Title.Should().Be("Premier League injury update");
    webSearchMetadata.Sources[0].Hostname.Should().Be("bbc.co.uk");
    webSearchMetadata.Sources[0].Url.Should().Be("https://bbc.co.uk/sport/injury");
    webSearchMetadata.Sources[1].Title.Should().Be("Arteta confirms Saka doubt");
    webSearchMetadata.Sources[1].Hostname.Should().Be("theguardian.com");
    webSearchMetadata.Sources[1].Url.Should().Be("https://theguardian.com/football/saka");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_GetWebGrounding_IncludesMetadataSource()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 21;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"websearch_getWebGrounding","arguments":[{"name":"query","value":"\"Arsenal form guide\""}]}""",
        Metadata = """
          {
            "sources": [
              {
                "title": "Grounding Title",
                "url": "https://wiki.example/page",
                "hostname": "wiki.example"
              }
            ]
          }
          """,
      },
    };

    SetupEmptyContext(sessionId);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Label.Should().Be(AgentToolCatalog.WebSearch.GetWebGrounding.DisplayName);
    result[messageId].Category.Should().Be("websearch");
    result[messageId].Details.Should().ContainSingle("Arsenal form guide");
    var webSearchMetadata = result[messageId].Metadata.Should().ContainSingle().Subject
      .Should().BeOfType<WebSearchSourcesToolCallMetadataDto>().Subject;
    webSearchMetadata.Sources.Should().ContainSingle();
    webSearchMetadata.Sources[0].Title.Should().Be("Grounding Title");
    webSearchMetadata.Sources[0].Hostname.Should().Be("wiki.example");
    webSearchMetadata.Sources[0].Url.Should().Be("https://wiki.example/page");
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_SearchNews_WithInvalidMetadata_ShowsQueryOnly()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 22;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"websearch_searchNews","arguments":[{"name":"query","value":"\"Arsenal injury news\""}]}""",
        Metadata = "not-json",
      },
    };

    SetupEmptyContext(sessionId);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Details.Should().ContainSingle("Arsenal injury news");
    result[messageId].Metadata.Should().BeNull();
  }

  [Fact]
  public async Task BuildDisplayByMessageIdAsync_NonWebSearchTool_IgnoresMetadata()
  {
    // Arrange
    const int sessionId = 1;
    const int messageId = 23;
    var messages = new List<AgentSessionMessage>
    {
      new()
      {
        Id = messageId,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = """{"name":"custom_unknownTool","arguments":[{"name":"query","value":"\"hello\""}]}""",
        Metadata = """
          {
            "sources": [
              {
                "title": "Should not appear",
                "url": "https://example.com",
                "hostname": "example.com"
              }
            ]
          }
          """,
      },
    };

    SetupEmptyContext(sessionId);

    // Act
    var result = await _sut.BuildDisplayByMessageIdAsync(sessionId, messages, CancellationToken.None);

    // Assert
    result[messageId].Details.Should().ContainSingle("hello");
    result[messageId].Metadata.Should().BeNull();
  }

  private void SetupEmptyContext(int sessionId)
  {
    _agentSessions.GetMatchIdsBySessionIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, int>());
    _betting.GetBetSlipsByAgentSessionIdAsync(sessionId, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSlip>());
    _matches.GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<Match>());
  }

  private static Match CreateMatch(int id, string homeName, string awayName) =>
    new()
    {
      Id = id,
      HomeClubId = id * 10,
      AwayClubId = id * 10 + 1,
      HomeClub = new DomainClub { Id = id * 10, Name = homeName },
      AwayClub = new DomainClub { Id = id * 10 + 1, Name = awayName },
    };
}
