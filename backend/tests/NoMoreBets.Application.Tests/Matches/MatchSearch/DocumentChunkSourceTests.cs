using System.Text.Json;
using FluentAssertions;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Domain.Players;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Matches.MatchSearch;

public class DocumentChunkSourceTests
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  [Fact]
  public void BuildEmbeddingText_MatchWithScore_IncludesMatchDetails()
  {
    // Arrange
    IDocumentChunkSource match = CreateMatch(homeGoals: 2, awayGoals: 1);

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be("Premier League 2025/26 | Arsenal vs Chelsea | 2026-03-15 | Finished | 2-1");
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
    text.Should().Be("Premier League 2025/26 | Arsenal vs Chelsea | 2026-03-15 | Upcomming");
  }

  [Fact]
  public void BuildEmbeddingText_MatchWithoutSeason_IncludesLeagueOnly()
  {
    // Arrange
    var match = CreateMatch(homeGoals: 1, awayGoals: 0);
    match.Stage = null;
    match.HomeClub.League = new League { Id = 7, Name = "Premier League", Slug = "premier-league" };

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be("Premier League | Arsenal vs Chelsea | 2026-03-15 | Finished | 1-0");
  }

  [Fact]
  public void BuildEmbeddingText_UnknownLeague_OmitsLeagueSeason()
  {
    // Arrange
    var match = CreateMatch(homeGoals: 1, awayGoals: 0);
    match.Stage!.Season = new Season
    {
      LeagueId = 8,
      Year = "N/A",
      League = new League
      {
        Id = 8,
        Name = "Unknown",
        Slug = League.UnknownSlug,
        SoccerdataId = League.UnknownSoccerdataId
      }
    };

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be("Arsenal vs Chelsea | 2026-03-15 | Finished | 1-0");
  }

  [Fact]
  public void BuildEmbeddingText_MatchWithLineupAndEvents_IncludesBoth()
  {
    // Arrange
    var match = CreateMatch(homeGoals: 2, awayGoals: 1);
    match.Lineup = new Lineup
    {
      MatchId = 1,
      HomeTeamJson = JsonSerializer.Serialize(new TeamLineup
      {
        LineupType = LineupType.Confirmed,
        Players = [new PlayerInLineup(FootballPosition.GK, "Raya"), new PlayerInLineup(FootballPosition.ST, "Saka")]
      }, JsonOptions),
      AwayTeamJson = JsonSerializer.Serialize(new TeamLineup
      {
        LineupType = LineupType.Predicted,
        Players = [new PlayerInLineup(FootballPosition.GK, "Sanchez")]
      }, JsonOptions)
    };
    match.MatchEvents =
    [
      CreateMatchEvent(match, match.HomeClub, "Saka", MatchEventType.Goal, 23),
      CreateMatchEvent(match, match.AwayClub, "Palmer", MatchEventType.YellowCard, 40),
      CreateMatchEvent(match, match.HomeClub, "Martinelli", MatchEventType.SubstitutionIn, 70)
    ];

    // Act
    var text = match.BuildEmbeddingText();

    // Assert
    text.Should().Be(
      "Premier League 2025/26 | Arsenal vs Chelsea | 2026-03-15 | Finished | 2-1"
      + " | Arsenal lineup: GK Raya, ST Saka"
      + " | Chelsea lineup: GK Sanchez"
      + " | Events: 23' Goal Saka, 40' YellowCard Palmer");
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
          Year = "2025/26",
          League = new League { Id = 7, Name = "Premier League", Slug = "premier-league" }
        }
      },
      HomeClub = new ClubEntity { Id = 10, Name = "Arsenal", LeagueId = 7 },
      AwayClub = new ClubEntity { Id = 20, Name = "Chelsea", LeagueId = 7 },
      MatchDate = new DateTime(2026, 3, 15, 15, 0, 0, DateTimeKind.Utc),
      MatchStatus = MatchStatus.Finished,
      HomeGoals = homeGoals,
      AwayGoals = awayGoals
    };

  private static MatchEvent CreateMatchEvent(
    Match match,
    ClubEntity club,
    string playerName,
    MatchEventType type,
    int minute) =>
    MatchEvent.Create(match.Id, club.Id, new Player { Name = playerName }, type, minute);
}
