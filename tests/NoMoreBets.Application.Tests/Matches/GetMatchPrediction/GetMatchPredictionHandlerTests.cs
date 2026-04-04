using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Matches.GetMatchPrediction;

public class GetMatchPredictionHandlerTests
{
  private readonly IMatchPrediction _matchPrediction = Substitute.For<IMatchPrediction>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly ILogger<GetMatchPredictionHandler> _logger = Substitute.For<ILogger<GetMatchPredictionHandler>>();
  private readonly GetMatchPredictionHandler _sut;

  public GetMatchPredictionHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchPredictionHandler(_matchPrediction, _unitOfWork, _logger);
  }

  [Fact]
  public async Task Handle_WhenMatchMissing_DoesNotInvokePrediction_AndReturnsUnit()
  {
    // Arrange
    _matches.GetMatchByIdAsync(99, Arg.Any<CancellationToken>())
      .Returns((Match?)null);

    // Act
    var result = await _sut.Handle(new GetMatchPredictionCommand(99), CancellationToken.None);

    // Assert
    result.Should().Be(Unit.Value);
    await _matchPrediction.DidNotReceive().InvokeAsync(Arg.Any<MatchPredictionPromptRequest>(), Arg.Any<CancellationToken>());
    await _matches.DidNotReceive().AddMatchAnalysisAsync(Arg.Any<MatchAnalysis>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchExists_PersistsAnalysisFromPrediction()
  {
    // Arrange
    var match = new Match
    {
      Id = 3,
      HomeClub = new ClubEntity { Name = "A" },
      AwayClub = new ClubEntity { Name = "B" }
    };
    _matches.GetMatchByIdAsync(3, Arg.Any<CancellationToken>())
      .Returns(match);
    _matchPrediction.InvokeAsync(Arg.Any<MatchPredictionPromptRequest>(), Arg.Any<CancellationToken>())
      .Returns("analysis text");

    // Act
    var result = await _sut.Handle(new GetMatchPredictionCommand(3), CancellationToken.None);

    // Assert
    result.Should().Be(Unit.Value);
    await _matches.Received(1).AddMatchAnalysisAsync(
      Arg.Is<MatchAnalysis>(a => a.MatchId == 3 && a.Code == "gpt-5.1" && a.Content == "analysis text"),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
