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
  public async Task Handle_WhenExcludeResearchFalse_MergesAndSortsDistinctMatches()
  {
    var utcNow = DateTime.UtcNow;
    var soonA = MatchAt(1, utcNow.AddHours(6));
    var duplicateSoon = MatchAt(2, utcNow.AddHours(12));
    var dataOnly = MatchAt(3, utcNow.AddHours(18));

    _matches.GetUpcomingMatchesReadyForPredictionAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { duplicateSoon, dataOnly });
    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { soonA, duplicateSoon });

    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(false), CancellationToken.None);

    result.Select(m => m.Id).Should().Equal(1, 2, 3);
    await _matches.DidNotReceive()
      .GetMatchIdsWithAnalysisCodeAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenExcludeResearchTrue_UsesWithoutResearchDataAndFiltersSoonByResearch()
  {
    var utcNow = DateTime.UtcNow;
    var dataComplete = MatchAt(10, utcNow.AddHours(4));
    var soonNoResearch = MatchAt(11, utcNow.AddHours(8));
    var soonWithResearch = MatchAt(12, utcNow.AddHours(10));

    _matches.GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { dataComplete });
    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { soonNoResearch, soonWithResearch });
    _matches.GetMatchIdsWithAnalysisCodeAsync(
        Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(11) && ids.Contains(12)),
        MatchAnalysis.StructuredResearchCode,
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 12 });

    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(), CancellationToken.None);

    result.Select(m => m.Id).Should().Equal(10, 11);
    await _matches.Received(1)
      .GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(Arg.Any<CancellationToken>());
    await _matches.DidNotReceive()
      .GetUpcomingMatchesReadyForPredictionAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSoonKickoffHasOutsideWindowMatch_ExcludesOutsideTwoDays()
  {
    var utcNow = DateTime.UtcNow;
    var withinWindow = MatchAt(20, utcNow.AddDays(1));
    var outsideWindow = MatchAt(21, utcNow.AddDays(3));

    _matches.GetUpcomingMatchesReadyForPredictionAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match>());
    _matches.GetUpcomingMatchesWithOddsSnapshotsAsync(Arg.Any<CancellationToken>())
      .Returns(new List<Match> { withinWindow, outsideWindow });

    var result = await _sut.Handle(new GetUpcomingMatchesReadyForPredictionQuery(false), CancellationToken.None);

    result.Select(m => m.Id).Should().Equal(20);
  }

  private static Match MatchAt(int id, DateTime date) =>
    new()
    {
      Id = id,
      MatchDate = date
    };
}
