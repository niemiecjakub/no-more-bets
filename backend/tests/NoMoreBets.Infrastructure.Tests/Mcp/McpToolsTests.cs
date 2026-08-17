using FluentAssertions;
using NoMoreBets.Infrastructure.Mcp;

namespace NoMoreBets.Infrastructure.Tests.Mcp;

public class McpToolsTests
{
  [Fact]
  public void McpToolCatalog_ListsReadOnlyToolsPerApplicationSlice()
  {
    var groups = McpToolCatalog.ListGroups();

    groups.Select(g => g.Id).Should().Equal("matches", "clubs", "leagues");

    groups.SelectMany(g => g.Tools).Select(t => t.Name).Should().BeEquivalentTo(
    [
      "matches_search",
      "matches_getLineups",
      "matches_getInjuries",
      "matches_getHeadToHeadStats",
      "matches_getEvents",
      "matches_getCurrentOdds",
      "matches_getResearch",
      "matches_getUpcomingResearched",
      "leagues_getList",
      "leagues_getTable",
      "leagues_getClubStatistics",
      "clubs_getList",
      "clubs_getById",
      "clubs_getMatches",
      "clubs_getNextMatch",
      "clubs_getRecentGames",
      "clubs_getRollingPerformance",
    ]);

    groups.SelectMany(g => g.Tools).Should().OnlyContain(t =>
      !string.IsNullOrWhiteSpace(t.Title) && !string.IsNullOrWhiteSpace(t.Description));
  }
}
