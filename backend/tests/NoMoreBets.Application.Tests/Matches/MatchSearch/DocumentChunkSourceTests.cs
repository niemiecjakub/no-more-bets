using FluentAssertions;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Matches.MatchSearch;

public class DocumentChunkSourceTests
{
  [Fact]
  public void BuildEmbeddingText_MatchWithScore_IncludesMatchDetails()
  {
    // Arrange
    IDocumentChunkSource match = CreateMatch(homeGoals: 2, awayGoals: 1);

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be("Premier League | Arsenal vs Chelsea | 2026-03-15 | Finished | 2-1");
  }

  [Fact]
  public void BuildEmbeddingText_MatchWithoutScore_OmitsScore()
  {
    // Arrange
    var match = CreateMatch(homeGoals: null, awayGoals: null);
    match.MatchStatus = MatchStatus.Upcomming;

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be("Premier League | Arsenal vs Chelsea | 2026-03-15 | Upcomming");
  }

  [Fact]
  public void BuildEmbeddingText_StructuredAnalysis_ReturnsResearchBodyOnly()
  {
    // Arrange
    var analysis = MatchAnalysis.CreateStructuredResearch(
      matchId: 1,
      agentSessionId: null,
      output: new MatchResearchOutput
      {
        MatchOverview = "Overview line",
        KeyPoints = ["Point A"],
        RisksAndUnknowns = []
      });
    analysis.Match = CreateMatch(homeGoals: 2, awayGoals: 1);

    // Act
    var text = analysis.BuildEmbeddingText();

    // Assert
    text.Should().Contain("Overview line");
    text.Should().Contain("Point A");
    text.Should().NotContain("Premier League");
  }

  [Fact]
  public void BuildEmbeddingText_AnalysisWithoutContent_ReturnsNull()
  {
    // Arrange
    var analysis = new MatchAnalysis
    {
      MatchId = 1,
      Code = MatchAnalysis.ResearchCode,
      Content = """{"text":""}"""
    };

    // Act
    var text = analysis.BuildEmbeddingText();

    // Assert
    text.Should().BeNull();
  }

  [Fact]
  public void BuildMetadata_Match_IncludesClubIdsAndLeagueId()
  {
    // Arrange
    IDocumentChunkSource match = CreateMatch(homeGoals: 2, awayGoals: 1);

    // Act
    var metadata = match.BuildMetadata();

    // Assert
    metadata.ClubIds.Should().BeEquivalentTo([10, 20]);
    metadata.LeagueId.Should().Be(7);
  }

  [Fact]
  public void BuildMetadata_Analysis_DelegatesToMatch()
  {
    // Arrange
    var analysis = MatchAnalysis.CreateStructuredResearch(
      matchId: 1,
      agentSessionId: null,
      output: new MatchResearchOutput
      {
        MatchOverview = "Overview",
        KeyPoints = [],
        RisksAndUnknowns = []
      });
    analysis.Match = CreateMatch(homeGoals: 1, awayGoals: 0);

    // Act
    var metadata = analysis.BuildMetadata();

    // Assert
    metadata.ClubIds.Should().BeEquivalentTo([10, 20]);
    metadata.LeagueId.Should().Be(7);
  }

  private static Match CreateMatch(int? homeGoals, int? awayGoals) =>
    new()
    {
      HomeClubId = 10,
      AwayClubId = 20,
      Stage = new Stage
      {
        Season = new Season
        {
          LeagueId = 7,
          League = new League { Id = 7, Name = "Premier League" }
        }
      },
      HomeClub = new ClubEntity { Id = 10, Name = "Arsenal", LeagueId = 7 },
      AwayClub = new ClubEntity { Id = 20, Name = "Chelsea", LeagueId = 7 },
      MatchDate = new DateTime(2026, 3, 15, 15, 0, 0, DateTimeKind.Utc),
      MatchStatus = MatchStatus.Finished,
      HomeGoals = homeGoals,
      AwayGoals = awayGoals
    };
}
