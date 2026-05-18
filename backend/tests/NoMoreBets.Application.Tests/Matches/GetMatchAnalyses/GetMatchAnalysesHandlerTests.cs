using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchAnalyses;
using NoMoreBets.Domain.Enums;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Matches.GetMatchAnalyses;

public class GetMatchAnalysesHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchAnalysesHandler _sut;

  public GetMatchAnalysesHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchAnalysesHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenMatchMissing_ReturnsNull()
  {
    _matches.GetMatchByIdAsync(99, Arg.Any<CancellationToken>()).Returns((DomainMatch?)null);

    var result = await _sut.Handle(new GetMatchAnalysesQuery(99), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_MapsAnalysesAndResearchSessionId()
  {
    var match = new DomainMatch
    {
      Id = 5,
      MatchDate = new DateTime(2026, 5, 10, 15, 0, 0, DateTimeKind.Utc),
      HomeClub = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea", Slug = "chelsea" },
      MatchStatusId = (int)MatchStatus.Upcomming,
      HomeGoals = null,
      AwayGoals = null,
    };
    _matches.GetMatchByIdAsync(5, Arg.Any<CancellationToken>()).Returns(match);
    _matches
      .GetLatestMatchAnalysisByCodeAsync(5, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(new MatchAnalysis { Id = 1, AgentSessionId = 42, Code = MatchAnalysis.ResearchCode, Content = "{}" });
    _matches
      .GetNonResearchAnalysesForMatchAsync(5, Arg.Any<CancellationToken>())
      .Returns(new List<MatchAnalysis>
      {
        new() { Id = 10, Code = "Tactics", Content = "raw text" },
      });

    var result = await _sut.Handle(new GetMatchAnalysesQuery(5), CancellationToken.None);

    result.Should().NotBeNull();
    result!.MatchId.Should().Be(5);
    result.HomeClubName.Should().Be("Arsenal");
    result.ResearchAgentSessionId.Should().Be(42);
    result.Analyses.Should().ContainSingle();
    result.Analyses[0].Code.Should().Be("Tactics");
    result.Analyses[0].Content.Should().Be("raw text");
    result.Analyses[0].Structured.Should().BeNull();
  }
}
