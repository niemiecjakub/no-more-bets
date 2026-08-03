using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Matches.GetMatchesPage;

public class GetMatchesPageHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly IEmbeddingService _embedding = Substitute.For<IEmbeddingService>();
  private readonly IDocumentChunkSearch _chunkSearch = Substitute.For<IDocumentChunkSearch>();
  private readonly GetMatchesPageHandler _sut;

  public GetMatchesPageHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _unitOfWork.Betting.Returns(_betting);
    _embedding.ModelId.Returns("text-embedding-3-small");
    _sut = new GetMatchesPageHandler(_unitOfWork, _mediator, _embedding, _chunkSearch);
  }

  [Fact]
  public async Task Handle_PassesFiltersAndCursorToRepository()
  {
    var cursorAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    var leagueIds = new[] { 1, 2 };
    _matches
      .GetMatchesPageAsync(
        10,
        3,
        leagueIds,
        cursorAt,
        5,
        MatchDateSortOrder.Descending,
        null,
        Arg.Is<IReadOnlyList<string>>(years => years.SequenceEqual(new[] { "2025-2026" })),
        Arg.Any<CancellationToken>())
      .Returns(new MatchPage(Array.Empty<DomainMatch>(), false));
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<DomainMatch>());

    await _sut.Handle(
      new GetMatchesPageQuery(10, 3, leagueIds, cursorAt, 5, SeasonYears: ["2025-2026"]),
      CancellationToken.None);

    await _matches.Received(1).GetMatchesPageAsync(
      10,
      3,
      leagueIds,
      cursorAt,
      5,
      MatchDateSortOrder.Descending,
      null,
      Arg.Is<IReadOnlyList<string>>(years => years.SequenceEqual(new[] { "2025-2026" })),
      Arg.Any<CancellationToken>());
    await _embedding.DidNotReceive().EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_SetsIsReadyToPredict_FromMediatorResult()
  {
    var match = CreateMatch(7, "Arsenal", "Chelsea");
    _matches
      .GetMatchesPageAsync(
        Arg.Any<int>(),
        Arg.Any<int?>(),
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<DateTime?>(),
        Arg.Any<int?>(),
        Arg.Any<MatchDateSortOrder>(),
        Arg.Any<string?>(),
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new MatchPage(new List<DomainMatch> { match }, false));
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<DomainMatch> { match });
    _matches.GetMatchIdsWithLineupAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int> { 7 });
    _matches.GetMatchIdsWithOddsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithHeadToHeadAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithAnalysisCodeAsync(Arg.Any<IReadOnlyCollection<int>>(), MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _betting.GetMatchIdsWithResearchPhaseSelectionsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());

    var result = await _sut.Handle(new GetMatchesPageQuery(10, null, [], null, null), CancellationToken.None);

    result.Items.Should().ContainSingle();
    result.Items[0].IsReadyToPredict.Should().BeTrue();
    result.Items[0].HasLineup.Should().BeTrue();
    result.HasMore.Should().BeFalse();
    result.NextCursorAt.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenHasMore_SetsNextCursorFromLastItem()
  {
    var older = CreateMatch(1, "A", "B", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
    var newer = CreateMatch(2, "C", "D", new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));
    _matches
      .GetMatchesPageAsync(
        Arg.Any<int>(),
        Arg.Any<int?>(),
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<DateTime?>(),
        Arg.Any<int?>(),
        Arg.Any<MatchDateSortOrder>(),
        Arg.Any<string?>(),
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new MatchPage(new List<DomainMatch> { newer, older }, true));
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<DomainMatch>());

    var result = await _sut.Handle(new GetMatchesPageQuery(1, null, [], null, null), CancellationToken.None);

    result.HasMore.Should().BeTrue();
    result.NextCursorAt.Should().Be(older.MatchDate);
    result.NextCursorId.Should().Be(older.Id);
  }

  [Fact]
  public async Task Handle_WhenSearchProvided_UsesHybridSearchAndPreservesRankOrder()
  {
    // Arrange
    var first = CreateMatch(10, "Arsenal", "Chelsea");
    var second = CreateMatch(20, "Liverpool", "Everton");
    _embedding.EmbedAsync("injuries", Arg.Any<CancellationToken>()).Returns([0.1f]);
    _chunkSearch
      .FindMatchIdsAsync("injuries", Arg.Any<float[]>(), "text-embedding-3-small", Arg.Any<CancellationToken>())
      .Returns(new[] { 20, 10 });
    _matches
      .GetMatchesByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
      .Returns(new List<DomainMatch> { first, second });
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<DomainMatch>());
    StubFlagLookups();

    // Act
    var result = await _sut.Handle(
      new GetMatchesPageQuery(10, null, [], null, null, Search: "injuries"),
      CancellationToken.None);

    // Assert
    result.Items.Select(i => i.Id).Should().Equal(20, 10);
    await _matches.DidNotReceive().GetMatchesPageAsync(
      Arg.Any<int>(),
      Arg.Any<int?>(),
      Arg.Any<IReadOnlyList<int>>(),
      Arg.Any<DateTime?>(),
      Arg.Any<int?>(),
      Arg.Any<MatchDateSortOrder>(),
      Arg.Any<string?>(),
      Arg.Any<IReadOnlyList<string>?>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenHybridSearchFails_FallsBackToKeywordSearch()
  {
    var match = CreateMatch(10, "Arsenal", "Chelsea");
    _embedding
      .EmbedAsync("Arsenal", Arg.Any<CancellationToken>())
      .Returns<Task<float[]>>(_ => throw new InvalidOperationException("embedding unavailable"));
    _matches
      .GetMatchesPageAsync(
        10,
        null,
        Arg.Any<IReadOnlyList<int>>(),
        null,
        null,
        MatchDateSortOrder.Descending,
        "Arsenal",
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new MatchPage(new List<DomainMatch> { match }, false));
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<DomainMatch>());
    StubFlagLookups();

    var result = await _sut.Handle(
      new GetMatchesPageQuery(10, null, [], null, null, Search: "Arsenal"),
      CancellationToken.None);

    result.Items.Should().ContainSingle().Which.Id.Should().Be(10);
    await _chunkSearch.DidNotReceive().FindMatchIdsAsync(
      Arg.Any<string>(),
      Arg.Any<float[]>(),
      Arg.Any<string>(),
      Arg.Any<CancellationToken>());
  }

  private void StubFlagLookups()
  {
    _matches.GetMatchIdsWithLineupAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithOddsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithHeadToHeadAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _matches.GetMatchIdsWithAnalysisCodeAsync(
        Arg.Any<IReadOnlyCollection<int>>(),
        MatchAnalysis.StructuredResearchCode,
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
    _betting.GetMatchIdsWithResearchPhaseSelectionsAsync(
        Arg.Any<IReadOnlyCollection<int>>(),
        Arg.Any<CancellationToken>())
      .Returns(new HashSet<int>());
  }

  private static DomainMatch CreateMatch(int id, string home, string away, DateTime? matchDate = null)
  {
    var status = new MatchStatusEntity { Id = (int)MatchStatus.Upcomming, Name = "Upcoming" };
    return new DomainMatch
    {
      Id = id,
      MatchDate = matchDate ?? DateTime.UtcNow,
      HomeClubId = 1,
      AwayClubId = 2,
      HomeClub = new ClubEntity { Name = home, Slug = home.ToLowerInvariant() },
      AwayClub = new ClubEntity { Name = away, Slug = away.ToLowerInvariant() },
      MatchStatusId = status.Id,
      MatchStatusEntity = status,
    };
  }
}
