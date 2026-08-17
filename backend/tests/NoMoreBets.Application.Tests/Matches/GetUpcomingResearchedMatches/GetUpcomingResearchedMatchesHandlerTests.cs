using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetUpcomingResearchedMatches;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Matches.GetUpcomingResearchedMatches;

public class GetUpcomingResearchedMatchesHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetUpcomingResearchedMatchesHandler _sut;

  public GetUpcomingResearchedMatchesHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetUpcomingResearchedMatchesHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoMatches_ReturnsEmptyList()
  {
    // Arrange
    _matches
      .GetUpcomingMatchesWithAnalysisCodeAsync(MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<DomainMatch>());

    // Act
    var result = await _sut.Handle(new GetUpcomingResearchedMatchesQuery(), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_WhenMatchesExist_MapsWithHasResearchTrue()
  {
    // Arrange
    var match = CreateMatch(42, "Home FC", "Away FC", new DateTime(2026, 6, 1, 15, 0, 0, DateTimeKind.Utc));
    _matches
      .GetUpcomingMatchesWithAnalysisCodeAsync(MatchAnalysis.StructuredResearchCode, Arg.Any<CancellationToken>())
      .Returns(new[] { match });

    // Act
    var result = await _sut.Handle(new GetUpcomingResearchedMatchesQuery(), CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    result[0].Id.Should().Be(42);
    result[0].HasResearch.Should().BeTrue();
    result[0].HomeClubName.Should().Be("Home FC");
    result[0].AwayClubName.Should().Be("Away FC");
  }

  private static DomainMatch CreateMatch(int id, string home, string away, DateTime matchDate)
  {
    var status = new MatchStatusEntity { Id = (int)MatchStatus.Upcomming, Name = "Upcoming" };
    return new DomainMatch
    {
      Id = id,
      MatchDate = matchDate,
      HomeClubId = 1,
      AwayClubId = 2,
      HomeClub = new ClubEntity { Name = home, Slug = home.ToLowerInvariant() },
      AwayClub = new ClubEntity { Name = away, Slug = away.ToLowerInvariant() },
      MatchStatusId = status.Id,
      MatchStatusEntity = status,
    };
  }
}
