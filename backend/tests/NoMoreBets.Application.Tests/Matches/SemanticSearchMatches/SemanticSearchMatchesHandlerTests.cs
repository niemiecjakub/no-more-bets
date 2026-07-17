using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Application.Matches.SemanticSearchMatches;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Matches.SemanticSearchMatches;

public class SemanticSearchMatchesHandlerTests
{
  private readonly IEmbeddingService _embedding = Substitute.For<IEmbeddingService>();
  private readonly IDocumentChunkSearch _chunkSearch = Substitute.For<IDocumentChunkSearch>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly SemanticSearchMatchesHandler _sut;

  public SemanticSearchMatchesHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _unitOfWork.Betting.Returns(_betting);
    _embedding.ModelId.Returns("text-embedding-3-small");
    _sut = new SemanticSearchMatchesHandler(_embedding, _chunkSearch, _unitOfWork, _mediator);
  }

  [Fact]
  public async Task Handle_WhenQueryEmpty_ReturnsEmptyWithoutCallingDependencies()
  {
    // Arrange / Act
    var result = await _sut.Handle(new SemanticSearchMatchesQuery("   "), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    await _embedding.DidNotReceive().EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _chunkSearch.DidNotReceive()
      .FindMatchIdsAsync(Arg.Any<string>(), Arg.Any<float[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoChunkHits_ReturnsEmpty()
  {
    // Arrange
    _embedding.EmbedAsync("injuries", Arg.Any<CancellationToken>()).Returns([0.1f, 0.2f]);
    _chunkSearch
      .FindMatchIdsAsync("injuries", Arg.Any<float[]>(), "text-embedding-3-small", Arg.Any<CancellationToken>())
      .Returns(Array.Empty<int>());

    // Act
    var result = await _sut.Handle(new SemanticSearchMatchesQuery("injuries"), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    await _matches.DidNotReceive()
      .GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PreservesRankOrderAndMapsDtoFlags()
  {
    // Arrange
    var first = CreateMatch(10, "Arsenal", "Chelsea");
    var second = CreateMatch(20, "Liverpool", "Everton");
    _embedding.EmbedAsync("defensive midfield", Arg.Any<CancellationToken>()).Returns([0.5f]);
    _chunkSearch
      .FindMatchIdsAsync("defensive midfield", Arg.Any<float[]>(), "text-embedding-3-small", Arg.Any<CancellationToken>())
      .Returns(new[] { 20, 10 });
    _matches
      .GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns(new List<DomainMatch> { first, second });
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<DomainMatch> { first });
    _matches.GetMatchIdsWithLineupAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 20 });
    _matches.GetMatchIdsWithOddsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithHeadToHeadAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 10 });
    _matches.GetMatchIdsWithAnalysisCodeAsync(
        Arg.Any<IReadOnlyCollection<int>>(),
        MatchAnalysis.StructuredResearchCode,
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 10 });
    _betting.GetMatchIdsWithResearchPhaseSelectionsAsync(
        Arg.Any<IReadOnlyCollection<int>>(),
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());

    // Act
    var result = await _sut.Handle(new SemanticSearchMatchesQuery("defensive midfield"), CancellationToken.None);

    // Assert
    result.Should().HaveCount(2);
    result[0].Id.Should().Be(20);
    result[0].HasLineup.Should().BeTrue();
    result[0].IsReadyToPredict.Should().BeFalse();
    result[1].Id.Should().Be(10);
    result[1].HasResearch.Should().BeTrue();
    result[1].HasHeadToHead.Should().BeTrue();
    result[1].IsReadyToPredict.Should().BeTrue();
  }

  [Fact]
  public async Task Handle_TrimsQueryBeforeEmbedding()
  {
    // Arrange
    _embedding.EmbedAsync("tight underdog", Arg.Any<CancellationToken>()).Returns([0.1f]);
    _chunkSearch
      .FindMatchIdsAsync(Arg.Any<string>(), Arg.Any<float[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<int>());

    // Act
    await _sut.Handle(new SemanticSearchMatchesQuery("  tight underdog  "), CancellationToken.None);

    // Assert
    await _embedding.Received(1).EmbedAsync("tight underdog", Arg.Any<CancellationToken>());
    await _chunkSearch.Received(1).FindMatchIdsAsync(
      "tight underdog",
      Arg.Any<float[]>(),
      "text-embedding-3-small",
      Arg.Any<CancellationToken>());
  }

  private static DomainMatch CreateMatch(int id, string home, string away)
  {
    var status = new MatchStatusEntity { Id = (int)MatchStatus.Upcomming, Name = "Upcoming" };
    return new DomainMatch
    {
      Id = id,
      MatchDate = DateTime.UtcNow,
      HomeClubId = 1,
      AwayClubId = 2,
      HomeClub = new ClubEntity { Name = home, Slug = home.ToLowerInvariant() },
      AwayClub = new ClubEntity { Name = away, Slug = away.ToLowerInvariant() },
      MatchStatusId = status.Id,
      MatchStatusEntity = status,
    };
  }
}
