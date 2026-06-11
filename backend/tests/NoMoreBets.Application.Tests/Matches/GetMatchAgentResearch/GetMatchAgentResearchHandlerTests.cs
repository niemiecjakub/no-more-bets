using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Tests.Matches.GetMatchAgentResearch;

public class GetMatchAgentResearchHandlerTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly ILogger<GetMatchAgentResearchHandler> _logger = Substitute.For<ILogger<GetMatchAgentResearchHandler>>();
  private readonly GetMatchAgentResearchHandler _sut;

  public GetMatchAgentResearchHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchAgentResearchHandler(_unitOfWork, _logger);
  }

  [Fact]
  public async Task Handle_WhenNoAnalysis_ReturnsNull()
  {
    _matches.GetLatestMatchAnalysisByCodeAsync(1, MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns((MatchAnalysis?)null);
    _matches.GetLatestMatchAnalysisByCodeAsync(1, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns((MatchAnalysis?)null);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenStructuredResearch_ReturnsDto()
  {
    var output = new MatchResearchOutput
    {
      MatchOverview = "Overview text",
      KeyPoints = ["Form is strong"],
      RisksAndUnknowns = ["Lineup uncertain"],
    };
    var content = JsonSerializer.Serialize(output, JsonOpts);
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 2,
      Code = MatchAnalysis.StructuredResearchCode,
      Content = content,
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(2, MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(2), CancellationToken.None);

    result.Should().NotBeNull();
    result!.MatchOverview.Should().Be("Overview text");
    result.KeyPoints.Should().Equal("Form is strong");
    result.RisksAndUnknowns.Should().Equal("Lineup uncertain");
  }

  [Fact]
  public async Task Handle_WhenLegacyResearchJson_FallsBackToDto()
  {
    var content = JsonSerializer.Serialize(new ResearchText("Report body"), JsonOpts);
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 2,
      Code = MatchAnalysis.ResearchCode,
      Content = content,
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(2, MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns((MatchAnalysis?)null);
    _matches.GetLatestMatchAnalysisByCodeAsync(2, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(2), CancellationToken.None);

    result.Should().NotBeNull();
    result!.MatchOverview.Should().Be("Report body");
    result.KeyPoints.Should().BeEmpty();
    result.RisksAndUnknowns.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_WhenInvalidJson_ReturnsNull()
  {
    var analysis = new MatchAnalysis
    {
      Id = 1,
      MatchId = 3,
      Code = MatchAnalysis.ResearchCode,
      Content = "not-json",
    };
    _matches.GetLatestMatchAnalysisByCodeAsync(3, MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns((MatchAnalysis?)null);
    _matches.GetLatestMatchAnalysisByCodeAsync(3, MatchAnalysis.ResearchCode, Arg.Any<CancellationToken>())
      .Returns(analysis);

    var result = await _sut.Handle(new GetMatchAgentResearchQuery(3), CancellationToken.None);

    result.Should().BeNull();
  }
}
