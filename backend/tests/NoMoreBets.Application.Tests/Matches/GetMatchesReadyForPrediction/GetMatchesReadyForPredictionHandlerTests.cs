using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Matches.GetMatchesReadyForPrediction;

public class GetMatchesReadyForPredictionHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();

  private readonly GetUpcomingMatchesReadyForPredictionHandler _sut;

  public GetMatchesReadyForPredictionHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetUpcomingMatchesReadyForPredictionHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenExcludeResearchFalse_ReturnsSoonKickoffSortedByDate()
  {
    // Arrange
    var utcNow = DateTime.UtcNow;
    var later = MatchAt(2, utcNow.AddHours(12));
    var sooner = MatchAt(1, utcNow.AddHours(6));

    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { later, sooner });

    // Act
    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(false), CancellationToken.None);

    // Assert
    result.Select(m => m.Id).Should().Equal(1, 2);
    await _matches.DidNotReceive()
      .GetMatchIdsWithAnalysisCodeAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenExcludeResearchTrue_FiltersOutMatchesWithExistingResearch()
  {
    // Arrange
    var utcNow = DateTime.UtcNow;
    var soonNoResearch = MatchAt(11, utcNow.AddHours(8));
    var soonWithResearch = MatchAt(12, utcNow.AddHours(10));

    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { soonNoResearch, soonWithResearch });
    _matches.GetMatchIdsWithAnalysisCodeAsync(
        Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(11) && ids.Contains(12)),
        MatchAnalysis.StructuredResearchCode,
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 12 });

    // Act
    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(), CancellationToken.None);

    // Assert
    result.Select(m => m.Id).Should().Equal(11);
  }

  [Fact]
  public async Task Handle_WhenSoonKickoffHasOutsideWindowMatch_ExcludesOutsideTwoDays()
  {
    // Arrange
    var utcNow = DateTime.UtcNow;
    var withinWindow = MatchAt(20, utcNow.AddDays(1));
    var outsideWindow = MatchAt(21, utcNow.AddDays(3));

    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { withinWindow, outsideWindow });

    // Act
    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(false), CancellationToken.None);

    // Assert
    result.Select(m => m.Id).Should().Equal(20);
  }

  private static Match MatchAt(int id, DateTime date) =>
    new()
    {
      Id = id,
      MatchDate = date
    };
}
