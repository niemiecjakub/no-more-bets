using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Matches.GetMatchAgentResearch;

public class GetMatchAgentResearchHandlerTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchAgentResearchHandler _sut;

  public GetMatchAgentResearchHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchAgentResearchHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoAnalysis_ReturnsNull()
  {
    _matches.GetLatestMatchAnalysisByCodeAsync(1, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns((MatchAnalysis?)null);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenValidResearchJson_ReturnsText()
  {
    var content = JsonSerializer.Serialize(new ResearchText("Report body"), JsonOpts);
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 2,
      Code = MatchAnalysis.ResearchCode,
      Content = content
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(2, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(2), CancellationToken.None);

    result.Should().Be("Report body");
  }

  [Fact]
  public async Task Handle_WhenInvalidJson_ReturnsNull()
  {
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 3,
      Code = MatchAnalysis.ResearchCode,
      Content = "not-json"
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(3, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(3), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenWrongCodeOnEntity_ReturnsNull()
  {
    var content = JsonSerializer.Serialize(new ResearchText("x"), JsonOpts);
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 4,
      Code = "other",
      Content = content
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(4, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(4), CancellationToken.None);

    result.Should().BeNull();
  }
}
