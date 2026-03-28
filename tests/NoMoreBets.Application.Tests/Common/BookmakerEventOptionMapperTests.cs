using System.Text.Json;
using FluentAssertions;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Common;

public class BookmakerEventOptionMapperTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private static string OptionOutcomesDir =>
    Path.Combine(AppContext.BaseDirectory, "optionOutcomes");

  private static BookmakerEvent LoadEvent(string fileName)
  {
    var path = Path.Combine(OptionOutcomesDir, fileName);
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<BookmakerEvent>(json, JsonOpts)
      ?? throw new InvalidOperationException($"Bad JSON: {fileName}");
  }

  private static Match CreateMatch(string homeName, string awayName) => new()
  {
    HomeClub = new ClubEntity { Name = homeName },
    AwayClub = new ClubEntity { Name = awayName }
  };

  [Fact]
  public void MapToRows_overunder_json_maps_all_total_goal_lines()
  {
    var ev = LoadEvent("overunder.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.OverUnderGoals);

    var match = CreateMatch("Everton", "Burnley");
    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, match, ev.Title);

    rows.Should().HaveCount(12);
    rows.Should().OnlyContain(r => r.EventOption.HasValue && r.Odds.HasValue);

    var expected = new[]
    {
      BettingEventOption.TotalGoals_Over_0_5,
      BettingEventOption.TotalGoals_Under_0_5,
      BettingEventOption.TotalGoals_Over_1_5,
      BettingEventOption.TotalGoals_Under_1_5,
      BettingEventOption.TotalGoals_Over_2_5,
      BettingEventOption.TotalGoals_Under_2_5,
      BettingEventOption.TotalGoals_Over_3_5,
      BettingEventOption.TotalGoals_Under_3_5,
      BettingEventOption.TotalGoals_Over_4_5,
      BettingEventOption.TotalGoals_Under_4_5,
      BettingEventOption.TotalGoals_Over_5_5,
      BettingEventOption.TotalGoals_Under_5_5
    };

    rows.Select(r => r.EventOption!.Value).Should().Equal(expected);
    rows.Should().OnlyContain(r => r.EventType == BettingEventType.OverUnderGoals);
  }

  [Fact]
  public void MapToRows_btts_json_maps_yes_no()
  {
    var ev = LoadEvent("btts.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.BothTeamsToScore);

    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, CreateMatch("a", "b"), ev.Title);
    rows.Should().HaveCount(2);
    rows[0].EventOption.Should().Be(BettingEventOption.BothTeamsToScore_Yes);
    rows[1].EventOption.Should().Be(BettingEventOption.BothTeamsToScore_No);
  }

  [Fact]
  public void MapToRows_matchresult_json_maps_home_draw_away()
  {
    var ev = LoadEvent("matchresult.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.MatchResult);

    var match = CreateMatch("Everton", "Burnley");
    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, match, ev.Title);
    rows.Should().HaveCount(3);
    rows[0].EventOption.Should().Be(BettingEventOption.MatchResult_Home);
    rows[1].EventOption.Should().Be(BettingEventOption.MatchResult_Draw);
    rows[2].EventOption.Should().Be(BettingEventOption.MatchResult_Away);
  }

  [Fact]
  public void MapToRows_doublechance_json_maps_three_outcomes()
  {
    var ev = LoadEvent("doublechance.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.DoubleChance);

    var match = CreateMatch("Everton", "Burnley");
    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, match, ev.Title);
    rows.Should().HaveCount(3);
    rows[0].EventOption.Should().Be(BettingEventOption.DoubleChance_HomeOrDraw);
    rows[1].EventOption.Should().Be(BettingEventOption.DoubleChance_HomeOrAway);
    rows[2].EventOption.Should().Be(BettingEventOption.DoubleChance_AwayOrDraw);
  }

  [Fact]
  public void MapToRows_handicap_json_maps_all_sample_lines()
  {
    var ev = LoadEvent("handicap.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.Handicap);

    var match = CreateMatch("Everton", "Burnley");
    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, match, ev.Title);
    rows.Should().HaveCount(ev.Options.Count);
    rows.Should().OnlyContain(r => r.EventOption.HasValue);
  }

  [Fact]
  public void MapToRows_exactscore_json_maps_known_scores_and_other()
  {
    var ev = LoadEvent("exactscore.json");
    var eventType = BookmakerEventTypeMapper.Map(ev.Title);
    eventType.Should().Be(BettingEventType.ExactScore);

    var match = CreateMatch("Everton", "Burnley");
    var rows = BookmakerEventOptionMapper.MapToRows(ev.Options, eventType!.Value, match, ev.Title);

    var byLabel = ev.Options.Zip(rows).ToDictionary(z => z.First.Label, z => z.Second.EventOption);

    byLabel["1 - 0"].Should().Be(BettingEventOption.CorrectScore_1_0);
    byLabel["0 - 0"].Should().Be(BettingEventOption.CorrectScore_0_0);
    byLabel["Inny"].Should().Be(BettingEventOption.CorrectScore_Other);
    byLabel["3 - 4"].Should().BeNull();
  }

  [Fact]
  public void MapToRows_team_goals_when_title_suffix_is_home_club_maps_home_team_lines()
  {
    var match = CreateMatch("Everton", "Burnley");
    var options = new List<EventOption>
    {
      new() { Label = "Powyżej 0,5", Odds = 2.1 },
      new() { Label = "Poniżej 2,5", Odds = 1.5 }
    };

    var rows = BookmakerEventOptionMapper.MapToRows(
      options,
      BettingEventType.TeamGoals,
      match,
      "Liczba goli - Everton");

    rows.Should().HaveCount(2);
    rows[0].EventOption.Should().Be(BettingEventOption.TeamGoals_Home_Over_0_5);
    rows[1].EventOption.Should().Be(BettingEventOption.TeamGoals_Home_Under_2_5);
  }

  [Fact]
  public void MapToRows_team_goals_when_title_suffix_is_away_club_does_not_map()
  {
    var match = CreateMatch("Everton", "Burnley");
    var options = new List<EventOption> { new() { Label = "Powyżej 0,5", Odds = 2.1 } };

    var rows = BookmakerEventOptionMapper.MapToRows(
      options,
      BettingEventType.TeamGoals,
      match,
      "Liczba goli - Burnley");

    rows[0].EventOption.Should().BeNull();
    rows[0].Odds.Should().BeNull();
  }

}
