using FluentAssertions;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Common;

public class BookmakerEventOptionMapperEdgeCaseTests
{
  private static Match CreateMatch(string homeName, string awayName) => new()
  {
    HomeClub = new ClubEntity { Name = homeName },
    AwayClub = new ClubEntity { Name = awayName }
  };

  private static BettingEventOption? MapSingle(string label, BettingEventType type, Match match) =>
    BookmakerEventOptionMapper.MapToRows([new EventOption { Label = label, Odds = 1 }], type, match)[0]
      .EventOption;

  public static TheoryData<string, string, bool> ClubNameFuzzyPairs => new()
  {
    { "Everton", "Everton FC", true },
    { "Everton FC", "Everton", true },
    { "Burnley", "Burnley F.C.", true },
    { "everton", "EVERTON", true },
    { "Norwich City", "Norwich", true },
    { "Everton", "Liverpool", false },
    { "Burnley", "Barnsley", false },
    { "Arsenal", "Chelsea", false },
    { "", "Everton", false },
    { "Everton", "", false }
  };

  [Theory]
  [MemberData(nameof(ClubNameFuzzyPairs))]
  public void ClubNameMatches_reflects_fuzzy_threshold(string bookmaker, string club, bool expected) =>
    BookmakerEventOptionMapper.ClubNameMatches(bookmaker, club).Should().Be(expected);

  [Fact]
  public void MatchResult_maps_when_label_is_short_form_of_db_name()
  {
    var match = CreateMatch("Everton FC", "Burnley FC");
    MapSingle("Everton", BettingEventType.MatchResult, match).Should().Be(BettingEventOption.MatchResult_Home);
    MapSingle("Burnley", BettingEventType.MatchResult, match).Should().Be(BettingEventOption.MatchResult_Away);
  }

  [Fact]
  public void MatchResult_maps_when_db_name_is_short_form_of_label()
  {
    var match = CreateMatch("Everton", "Burnley");
    MapSingle("Everton FC", BettingEventType.MatchResult, match).Should().Be(BettingEventOption.MatchResult_Home);
  }

  [Fact]
  public void MatchResult_does_not_confuse_unrelated_clubs()
  {
    var match = CreateMatch("Everton", "Burnley");
    MapSingle("Liverpool", BettingEventType.MatchResult, match).Should().BeNull();
  }

  [Fact]
  public void DoubleChance_fuzzy_home_draw_suffix()
  {
    var match = CreateMatch("Everton FC", "Burnley FC");
    MapSingle("Everton lub remis", BettingEventType.DoubleChance, match)
      .Should().Be(BettingEventOption.DoubleChance_HomeOrDraw);
  }

  [Fact]
  public void DoubleChance_fuzzy_away_draw_prefix()
  {
    var match = CreateMatch("Everton FC", "Burnley FC");
    MapSingle("Remis lub Burnley", BettingEventType.DoubleChance, match)
      .Should().Be(BettingEventOption.DoubleChance_AwayOrDraw);
  }

  [Fact]
  public void DoubleChance_fuzzy_home_away_pair()
  {
    var match = CreateMatch("Everton FC", "Burnley FC");
    MapSingle("Everton lub Burnley", BettingEventType.DoubleChance, match)
      .Should().Be(BettingEventOption.DoubleChance_HomeOrAway);
  }

  [Fact]
  public void Handicap_fuzzy_team_on_line_and_in_draw()
  {
    var match = CreateMatch("Everton FC", "Burnley FC");
    MapSingle("Everton (-2)", BettingEventType.Handicap, match)
      .Should().Be(BettingEventOption.Handicap_Home_Minus_2);
    MapSingle("Burnley (+2)", BettingEventType.Handicap, match)
      .Should().Be(BettingEventOption.Handicap_Away_Plus_2);
    MapSingle("Remis (Everton -2)", BettingEventType.Handicap, match)
      .Should().Be(BettingEventOption.Handicap_Draw_Minus_2);
  }

  [Fact]
  public void OverUnder_invalid_line_without_separator_returns_null()
  {
    var match = CreateMatch("a", "b");
    MapSingle("Powyżej 05", BettingEventType.OverUnderGoals, match).Should().BeNull();
    MapSingle("Powyżej", BettingEventType.OverUnderGoals, match).Should().BeNull();
  }

  [Fact]
  public void ExactScore_rejects_negative_goals_in_label()
  {
    var match = CreateMatch("a", "b");
    MapSingle("1 - -1", BettingEventType.ExactScore, match).Should().BeNull();
  }

  [Fact]
  public void Btts_unknown_label_returns_null()
  {
    var match = CreateMatch("a", "b");
    MapSingle("Maybe", BettingEventType.BothTeamsToScore, match).Should().BeNull();
  }
}
