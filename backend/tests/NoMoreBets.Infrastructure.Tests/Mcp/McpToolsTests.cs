using System.Reflection;
using FluentAssertions;
using MediatR;
using ModelContextProtocol.Server;
using NSubstitute;
using NoMoreBets.Infrastructure.Mcp.Tools;

namespace NoMoreBets.Infrastructure.Tests.Mcp;

public class McpToolsTests
{
  private readonly IMediator _mediator = Substitute.For<IMediator>();

  [Fact]
  public void McpTools_ExposeReadOnlyToolsPerApplicationSlice()
  {
    var tools = BuildTools();

    tools.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(
    [
      "matches_search",
      "matches_getLineups",
      "matches_getInjuries",
      "matches_getHeadToHeadStats",
      "matches_getEvents",
      "matches_getCurrentOdds",
      "matches_getResearch",
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

    tools.Should().OnlyContain(t => t.ProtocolTool.Annotations!.ReadOnlyHint == true);
    tools.Should().OnlyContain(t => t.ProtocolTool.Description != null);
  }

  private List<McpServerTool> BuildTools()
  {
    object[] targets = [new MatchesMcpTools(_mediator), new LeaguesMcpTools(_mediator), new ClubsMcpTools(_mediator)];

    return targets
      .SelectMany(target => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
        // Create() generates the JSON input schema, so it fails on unsupported parameter types.
        .Select(m => McpServerTool.Create(m, target)))
      .ToList();
  }
}
