using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Tests.Features.MatchAnalysis;

public class MatchMatcherTests
{
  private readonly MatchMatcher _sut;
  public MatchMatcherTests()
  {
    var logger = Mock.Of<ILogger<MatchMatcher>>();
    _sut = new MatchMatcher(logger);
  }

  [Fact]
  public void BuildLineupIndex_WithOneLineup_ReturnsDictionaryWithCorrectKey()
  {
    // Arrange
    var lineup = new GameLineup
    {
      Date = "2026-01-15",
      Time = "15:00",
      HomeTeam = new TeamLineup
      {
        TeamName = "Arsenal",
        LineupType = LineupType.Predicted,
        Players = [],
        Injuries = []
      },
      AwayTeam = new TeamLineup
      {
        TeamName = "Chelsea",
        LineupType = LineupType.Predicted,
        Players = [],
        Injuries = []
      }
    };
    var lineups = new List<GameLineup> { lineup };

    // Act
    var index = _sut.BuildLineupIndex(lineups);

    // Assert
    index.Should().HaveCount(1);
    var key = new TeamKey("Arsenal", "Chelsea");
    index.Should().ContainKey(key);
    index[key].Should().BeSameAs(lineup);
  }

  [Fact]
  public void BuildLineupIndex_WithReversedTeamNames_ProducesSameKey()
  {
    // Arrange
    var lineup1 = new GameLineup
    {
      Date = "2026-01-15",
      HomeTeam = new TeamLineup { TeamName = "Arsenal", LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { TeamName = "Chelsea", LineupType = LineupType.Predicted, Players = [], Injuries = [] }
    };
    var lineup2 = new GameLineup
    {
      Date = "2026-01-15",
      HomeTeam = new TeamLineup { TeamName = "Chelsea", LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { TeamName = "Arsenal", LineupType = LineupType.Predicted, Players = [], Injuries = [] }
    };
    var lineups = new List<GameLineup> { lineup1, lineup2 };

    // Act
    var index = _sut.BuildLineupIndex(lineups);

    // Assert
    index.Should().HaveCount(1);
    var keyArsenalChelsea = new TeamKey("Arsenal", "Chelsea");
    var keyChelseaArsenal = new TeamKey("Chelsea", "Arsenal");
    keyArsenalChelsea.Should().Be(keyChelseaArsenal);
    index.Should().ContainKey(keyArsenalChelsea);
  }

  [Fact]
  public void FindLineup_WithExactMatch_ReturnsLineup()
  {
    // Arrange
    var lineup = new GameLineup
    {
      Date = "2026-01-15",
      Time = "15:00",
      HomeTeam = new TeamLineup { TeamName = "Arsenal", LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { TeamName = "Chelsea", LineupType = LineupType.Predicted, Players = [], Injuries = [] }
    };
    var index = _sut.BuildLineupIndex(new List<GameLineup> { lineup });
    var match = new UpcomingMatchPreview
    {
      Id = 1,
      Date = "15/01/2026",
      Time = "15:00",
      ExcitementRating = 7.5,
      Teams = new Teams
      {
        Home = new TeamInfo { Id = 1, Name = "Arsenal" },
        Away = new TeamInfo { Id = 2, Name = "Chelsea" }
      }
    };

    // Act
    var result = _sut.FindLineup(match.Teams.Home.Name, match.Teams.Away.Name, index);

    // Assert
    result.Should().NotBeNull();
    result!.HomeTeam.TeamName.Should().Be("Arsenal");
    result.AwayTeam.TeamName.Should().Be("Chelsea");
  }

  [Fact]
  public void FindLineup_WithNoMatch_ReturnsNull()
  {
    // Arrange: empty index so no exact or fuzzy match
    var index = _sut.BuildLineupIndex([]);
    var match = new UpcomingMatchPreview
    {
      Id = 1,
      Date = "15/01/2026",
      Time = "15:00",
      ExcitementRating = 0,
      Teams = new Teams
      {
        Home = new TeamInfo { Id = 1, Name = "Manchester United" },
        Away = new TeamInfo { Id = 2, Name = "Liverpool" }
      }
    };

    // Act
    var result = _sut.FindLineup(match.Teams.Home.Name, match.Teams.Away.Name, index);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindSoccerDataMatch_WithExactMatch_ReturnsMatch()
  {
    // Arrange
    var leagues = new List<LeagueMatchPreviews>
        {
            new()
            {
                LeagueId = 228,
                LeagueName = "Premier League",
                MatchPreviews =
                [
                    new UpcomingMatchPreview
                    {
                        Id = 100,
                        Date = "15/01/2026",
                        Time = "15:00",
                        ExcitementRating = 7.5,
                        Teams = new Teams
                        {
                            Home = new TeamInfo { Id = 1, Name = "Arsenal" },
                            Away = new TeamInfo { Id = 2, Name = "Chelsea" }
                        }
                    }
                ]
            }
        };

    // Act
    var result = _sut.FindSoccerDataMatch("Arsenal", "Chelsea", leagues);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(100);
    result.Teams.Home.Name.Should().Be("Arsenal");
    result.Teams.Away.Name.Should().Be("Chelsea");
  }

  [Fact]
  public void FindSoccerDataMatch_WithNoMatch_ReturnsNull()
  {
    // Arrange: empty leagues so no exact or fuzzy match
    var leagues = new List<LeagueMatchPreviews>();

    // Act
    var result = _sut.FindSoccerDataMatch("Manchester United", "Liverpool", leagues);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindFotmobClub_WithExactMatch_ReturnsClub()
  {
    // Arrange
    var clubs = new List<ClubDto>
        {
            new(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, "WWWDW", null, null, null),
            new(2, "Chelsea", "CHE", 43, "", 20, 12, 4, 4, 38, 25, "+13", 40, "WLWDW", null, null, null)
        };

    // Act
    var result = _sut.FindFotmobClub("Arsenal", clubs);

    // Assert
    result.Should().NotBeNull();
    result!.TeamName.Should().Be("Arsenal");
  }

  [Fact]
  public void FindFotmobClub_WithNoMatch_ReturnsNull()
  {
    // Arrange: one club that does not match; fuzzy score is below cutoff so returns null
    var clubs = new List<ClubDto>
        {
            new(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, "WWWDW", null, null, null)
        };
    // Use a name that will not exact/partial match and will have low fuzzy score
    const string teamName = "XYZ Unknown Team 123";

    // Act
    var result = _sut.FindFotmobClub(teamName, clubs);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindFotmobClub_WithEmptyList_ReturnsNull()
  {
    // Arrange
    var clubs = new List<ClubDto>();

    // Act
    var result = _sut.FindFotmobClub("Arsenal", clubs);

    // Assert
    result.Should().BeNull();
  }
}
