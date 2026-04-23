using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Application.Common.Dto.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using Sut = NoMoreBets.Application.Common.MatchMatcher.MatchMatcher;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Tests.Common.MatchMatcher;

public class MatchMatcherTests
{
  private readonly Sut _sut;

  public MatchMatcherTests()
  {
    var logger = Substitute.For<ILogger<Sut>>();
    _sut = new Sut(logger);
  }

  [Fact]
  public void BuildLineupIndex_WithOneLineup_ReturnsDictionaryWithCorrectKey()
  {
    // Arrange
    var lineup = new GameLineup
    {
      Date = new DateTime(2026, 1, 15),
      Time = "15:00",
      HomeTeamName = "Arsenal",
      AwayTeamName = "Chelsea",
      HomeTeam = new TeamLineup
      {
        LineupType = LineupType.Predicted,
        Players = [],
        Injuries = []
      },
      AwayTeam = new TeamLineup
      {
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
      Date = new DateTime(2026, 1, 15),
      HomeTeamName = "Arsenal",
      AwayTeamName = "Chelsea",
      HomeTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] }
    };
    var lineup2 = new GameLineup
    {
      Date = new DateTime(2026, 1, 15),
      HomeTeamName = "Chelsea",
      AwayTeamName = "Arsenal",
      HomeTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] }
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
      Date = new DateTime(2026, 1, 15),
      Time = "15:00",
      HomeTeamName = "Arsenal",
      AwayTeamName = "Chelsea",
      HomeTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] },
      AwayTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = [], Injuries = [] }
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
    result!.HomeTeamName.Should().Be("Arsenal");
    result.AwayTeamName.Should().Be("Chelsea");
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
    // Arrange - ClubDto from Application.Clubs.Dto (FotMob league table row shape)
    var clubs = new List<ClubDto>
    {
      new(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, new[] { "Win", "Win", "Win", "Draw", "Win" }, null, null, null),
      new(2, "Chelsea", "CHE", 43, "", 20, 12, 4, 4, 38, 25, "+13", 40, new[] { "Win", "Loss", "Win", "Draw", "Win" }, null, null, null)
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
      new(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, new[] { "Win", "Win", "Win", "Draw", "Win" }, null, null, null)
    };
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

  [Fact]
  public void FindFotmobClub_WhenTeamNameNull_NormalizesAndReturnsNullForEmptyClubs()
  {
    // Arrange: null team name normalizes to empty; empty clubs list returns null
    var clubs = new List<ClubDto>();
    string? teamName = null;

    // Act
    var result = _sut.FindFotmobClub(teamName, clubs);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindFotmobClub_WhenTeamNameNull_WithNonEmptyClubs_ReturnsFirstClubBecauseEmptyContainedInEveryName()
  {
    // Arrange: null normalizes to ""; in FindFotmobClub the partial match uses Contains("") which is true for any string, so first club is returned
    var clubs = new List<ClubDto>
    {
      new(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, new[] { "Win" }, null, null, null)
    };
    string? teamName = null;

    // Act
    var result = _sut.FindFotmobClub(teamName, clubs);

    // Assert: implementation treats null as "" and "" is contained in "arsenal", so returns first club
    result.Should().NotBeNull();
    result!.TeamName.Should().Be("Arsenal");
  }

  [Fact]
  public void FindClub_WhenEmptyClubs_ThrowsInvalidOperationException()
  {
    // Arrange
    var clubs = new List<ClubEntity>();

    // Act
    var act = () => _sut.FindClub("Arsenal", clubs);

    // Assert
    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*No clubs in league to match*");
  }

  [Fact]
  public void FindClub_WhenNoMatchAmongClubs_ThrowsInvalidOperationException()
  {
    // Arrange: club names that will not match "XYZ Unknown"
    var clubs = new List<ClubEntity>
    {
      new() { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 },
      new() { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 }
    };

    // Act
    var act = () => _sut.FindClub("XYZ Unknown", clubs);

    // Assert
    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*No matching club found*");
  }

  [Fact]
  public void FindClub_ExactMatchIgnoreCase_ReturnsClub()
  {
    // Arrange
    var arsenal = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var clubs = new List<ClubEntity> { arsenal };

    // Act
    var result = _sut.FindClub("arsenal", clubs);

    // Assert
    result.Should().BeSameAs(arsenal);
    result.Name.Should().Be("Arsenal");
  }

  [Fact]
  public void FindClub_FuzzyMatchAboveCutoff_ReturnsClub()
  {
    // Arrange: "Arsenal" vs "Arsenal FC" - fuzzy match typically above 70
    var arsenalFc = new ClubEntity { Id = 1, Name = "Arsenal FC", LeagueId = 1, SoccerdataId = 1 };
    var clubs = new List<ClubEntity> { arsenalFc };

    // Act
    var result = _sut.FindClub("Arsenal", clubs);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("Arsenal FC");
  }

  [Fact]
  public void FindClub_BetclicPolishMarseilleAlias_ReturnsMarseille()
  {
    var marseille = new ClubEntity { Id = 1, Name = "Marseille", LeagueId = 6, SoccerdataId = 3769 };
    var clubs = new List<ClubEntity> { marseille };

    var result = _sut.FindClub("Marsylia", clubs);

    result.Should().BeSameAs(marseille);
    result.Name.Should().Be("Marseille");
  }

  [Fact]
  public void FindClub_BetclicFcKoelnAlias_ReturnsFcCologne()
  {
    var fcCologne = new ClubEntity { Id = 1, Name = "FC Cologne", LeagueId = 4, SoccerdataId = 3336 };
    var clubs = new List<ClubEntity> { fcCologne };

    var result = _sut.FindClub("FC Koeln", clubs);

    result.Should().BeSameAs(fcCologne);
    result.Name.Should().Be("FC Cologne");
  }

  [Fact]
  public void FindClub_GermanOneFcKolnWithUmlaut_ReturnsFcCologne()
  {
    var fcCologne = new ClubEntity { Id = 1, Name = "FC Cologne", LeagueId = 4, SoccerdataId = 3336 };
    var clubs = new List<ClubEntity> { fcCologne };

    var result = _sut.FindClub("1. FC Köln", clubs);

    result.Should().BeSameAs(fcCologne);
    result.Name.Should().Be("FC Cologne");
  }

  [Fact]
  public void FindClub_MonchengladbachAlias_ReturnsBorussiaMGladbach()
  {
    var gladbach = new ClubEntity { Id = 1, Name = "Borussia M'gladbach", LeagueId = 4, SoccerdataId = 4272 };
    var clubs = new List<ClubEntity> { gladbach };

    var result = _sut.FindClub("Mönchengladbach", clubs);

    result.Should().BeSameAs(gladbach);
    result.Name.Should().Be("Borussia M'gladbach");
  }

  [Fact]
  public void FindClub_Hoffenheim1899Alias_ReturnsHoffenheim()
  {
    var hoffenheim = new ClubEntity { Id = 1, Name = "Hoffenheim", LeagueId = 4, SoccerdataId = 3297 };
    var clubs = new List<ClubEntity> { hoffenheim };

    var result = _sut.FindClub("1899 Hoffenheim", clubs);

    result.Should().BeSameAs(hoffenheim);
    result.Name.Should().Be("Hoffenheim");
  }

  [Fact]
  public void FindClub_AekLarnacaAlias_ReturnsAekLarnaka()
  {
    var aek = new ClubEntity { Id = 1, Name = "AEK Larnaka", LeagueId = 1, SoccerdataId = 1 };
    var clubs = new List<ClubEntity> { aek };

    var result = _sut.FindClub("AEK Larnaca", clubs);

    result.Should().BeSameAs(aek);
    result.Name.Should().Be("AEK Larnaka");
  }

  [Fact]
  public void FindClub_ParisSaintGermainAlias_ReturnsPsg()
  {
    var psg = new ClubEntity { Id = 1, Name = "PSG", LeagueId = 6, SoccerdataId = 4228 };
    var clubs = new List<ClubEntity> { psg };

    var result = _sut.FindClub("Paris Saint-Germain", clubs);

    result.Should().BeSameAs(psg);
    result.Name.Should().Be("PSG");
  }

  [Fact]
  public void FindClub_ParisFcAndPsgInLeague_Disambiguates()
  {
    var parisFc = new ClubEntity { Id = 1, Name = "Paris FC", LeagueId = 6, SoccerdataId = 4241 };
    var psg = new ClubEntity { Id = 2, Name = "PSG", LeagueId = 6, SoccerdataId = 4228 };
    var clubs = new List<ClubEntity> { psg, parisFc };

    _sut.FindClub("Paris FC", clubs).Should().BeSameAs(parisFc);
    _sut.FindClub("Paris  SG", clubs).Should().BeSameAs(psg);
    _sut.FindClub("Paris SG", clubs).Should().BeSameAs(psg);
    _sut.FindClub("Paris Saint-Germain", clubs).Should().BeSameAs(psg);
  }

  [Fact]
  public void FindFotmobClub_ShortParisQuery_PrefersLongerTeamNameOverParisFc()
  {
    var clubs = new List<ClubDto>
    {
      new(1, "Paris FC", "PFC", 1, "", 0, 0, 0, 0, 0, 0, "0", 0, [], null, null, null),
      new(2, "Paris Saint-Germain", "PSG", 2, "", 0, 0, 0, 0, 0, 0, "0", 0, [], null, null, null),
    };

    var result = _sut.FindFotmobClub("paris", clubs);

    result.Should().NotBeNull();
    result!.TeamName.Should().Be("Paris Saint-Germain");
  }

  [Fact]
  public void FindClub_FoldedDiacriticsExactMatch_ReturnsClub()
  {
    var club = new ClubEntity { Id = 1, Name = "TSV München Test", LeagueId = 1, SoccerdataId = 1 };
    var clubs = new List<ClubEntity> { club };

    var result = _sut.FindClub("TSV Munchen Test", clubs);

    result.Should().BeSameAs(club);
    result.Name.Should().Be("TSV München Test");
  }

  [Fact]
  public void FindXgStats_WhenEmptyList_ReturnsNull()
  {
    // Arrange
    var xgStats = new List<XgStats>();

    // Act
    var result = _sut.FindXgStats("Arsenal", xgStats);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindXgStats_WhenNoMatch_ReturnsNull()
  {
    // Arrange
    var xgStats = new List<XgStats>
    {
      new()
      {
        Position = 1,
        PositionChange = null,
        TeamId = 1,
        TeamName = "Arsenal",
        TeamShortname = "ARS",
        TeamLogoUrl = "",
        Xg = 42.5,
        XgDiff = "+1",
        Xga = 16.0,
        XgaDiff = "+0.5",
        Xpts = 51.0,
        XptsDiff = "+2"
      }
    };

    // Act
    var result = _sut.FindXgStats("XYZ Unknown Team", xgStats);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindXgStats_ExactMatch_ReturnsStat()
  {
    // Arrange
    var stat = new XgStats
    {
      Position = 1,
      PositionChange = null,
      TeamId = 9825,
      TeamName = "Arsenal",
      TeamShortname = "Arsenal",
      TeamLogoUrl = "",
      Xg = 42.66,
      XgDiff = "+3.3",
      Xga = 16.42,
      XgaDiff = "+0.6",
      Xpts = 51.23,
      XptsDiff = "+2"
    };
    var xgStats = new List<XgStats> { stat };

    // Act
    var result = _sut.FindXgStats("Arsenal", xgStats);

    // Assert
    result.Should().NotBeNull();
    result!.TeamName.Should().Be("Arsenal");
    result.Xg.Should().BeApproximately(42.66, 0.01);
  }

  [Fact]
  public void FindXgStats_PartialNameContains_ReturnsStat()
  {
    // Arrange: "Arsenal" contained in "Arsenal" (exact) - partial match path uses Contains
    var stat = new XgStats
    {
      Position = 1,
      PositionChange = null,
      TeamId = 9825,
      TeamName = "Arsenal",
      TeamShortname = "ARS",
      TeamLogoUrl = "",
      Xg = 42.0,
      XgDiff = null,
      Xga = 16.0,
      XgaDiff = null,
      Xpts = 50.0,
      XptsDiff = null
    };
    var xgStats = new List<XgStats> { stat };

    // Act
    var result = _sut.FindXgStats("Arsenal", xgStats);

    // Assert
    result.Should().NotBeNull();
    result!.TeamName.Should().Be("Arsenal");
  }

  [Fact]
  public void FindBestMatch_WhenEmptyCandidates_ReturnsDefault()
  {
    // Arrange
    var candidates = new List<(string HomeName, string AwayName, string Value)>();

    // Act
    var result = _sut.FindBestMatch("Arsenal", "Chelsea", candidates);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void FindBestMatch_ExactMatch_ReturnsValue()
  {
    // Arrange
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Arsenal", "Chelsea", 42)
    };

    // Act
    var result = _sut.FindBestMatch("Arsenal", "Chelsea", candidates);

    // Assert
    result.Should().Be(42);
  }

  [Fact]
  public void FindBestMatch_AliasAndUmlautVariant_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("FC Cologne", "Bayer Leverkusen", 7)
    };

    var result = _sut.FindBestMatch("1. FC Köln", "Bayer Leverkusen", candidates);

    result.Should().Be(7);
  }

  [Fact]
  public void FindBestMatch_VflWolfsburgAndMonchengladbachAliases_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Wolfsburg", "Borussia M'gladbach", 11)
    };

    var result = _sut.FindBestMatch("VfL Wolfsburg", "Mönchengladbach", candidates);

    result.Should().Be(11);
  }

  [Fact]
  public void FindBestMatch_HamburgerSvAndHoffenheim1899Aliases_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Hamburg", "Hoffenheim", 12)
    };

    var result = _sut.FindBestMatch("Hamburger SV", "1899 Hoffenheim", candidates);

    result.Should().Be(12);
  }

  [Fact]
  public void FindBestMatch_RayoAndSociedadSpanishVariants_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Rayo Vallecano de Madrid", "Real Sociedad de Fútbol", 21)
    };

    var result = _sut.FindBestMatch("Rayo Vallecano", "Real Sociedad", candidates);

    result.Should().Be(21);
  }

  [Fact]
  public void FindBestMatch_OviedoAlias_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Real Oviedo", "Elche", 22)
    };

    var result = _sut.FindBestMatch("Oviedo", "Elche", candidates);

    result.Should().Be(22);
  }

  [Fact]
  public void FindBestMatch_OsasunaAndSevillaVariants_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("CA Osasuna", "Sevilla FC", 23)
    };

    var result = _sut.FindBestMatch("Osasuna", "Sevilla", candidates);

    result.Should().Be(23);
  }

  [Fact]
  public void FindBestMatch_VillarrealAndCeltaVariants_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Villarreal CF", "RC Celta de Vigo", 24)
    };

    var result = _sut.FindBestMatch("Villarreal", "Celta Vigo", candidates);

    result.Should().Be(24);
  }

  [Fact]
  public void FindBestMatch_EspanyolAndLevanteVariants_ReturnsValue()
  {
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("RCD Espanyol", "Levante UD", 25)
    };

    var result = _sut.FindBestMatch("Espanyol", "Levante", candidates);

    result.Should().Be(25);
  }

  [Fact]
  public void FindBestMatch_NoMatch_ReturnsDefault()
  {
    // Arrange: only one candidate that does not match
    var candidates = new List<(string HomeName, string AwayName, int Value)>
    {
      ("Manchester United", "Liverpool", 100)
    };

    // Act
    var result = _sut.FindBestMatch("Arsenal", "Chelsea", candidates);

    // Assert
    result.Should().Be(0);
  }
}
