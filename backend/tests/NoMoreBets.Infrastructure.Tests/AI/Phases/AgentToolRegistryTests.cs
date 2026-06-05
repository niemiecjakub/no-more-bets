using FluentAssertions;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Phases;

public class AgentToolRegistryTests
{
  private readonly IPluginFactory _pluginFactory;

  public AgentToolRegistryTests()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var mediator = Substitute.For<IMediator>();
    var xApiService = Substitute.For<IXApiService>();
    var agentSessionContext = new AgentSessionContext();

    var sp = new ServiceCollection()
      .AddSingleton(unitOfWork)
      .AddSingleton(mediator)
      .AddSingleton(xApiService)
      .AddSingleton(agentSessionContext)
      .BuildServiceProvider();

    _pluginFactory = new PluginFactory(sp);
  }

  [Fact]
  public void ResearchPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = _pluginFactory.ResolveTools([
      Tools.Match.GetLineups,
      Tools.Match.GetInjuries,
      Tools.Match.GetHead2HeadStats,
      Tools.Match.GetClubDailySummary,
      Tools.Match.GetClubRecentGames,
      Tools.Match.GetClubLeagueStatistics,
      Tools.Match.GetLeagueTable,
      Tools.Match.GetMatchBettingOddsHistory,
      Tools.Match.GetClubRollingPerformance,
      Tools.Match.SaveMatchAnalysis,
    ]);

    tools.Should().HaveCount(10);
    ToolNames(tools).Should().Contain(
    [
      "GetLineups",
      "SaveMatchAnalysisAsync",
    ]);
  }

  [Fact]
  public void ResearchPaperBetStepTools_RegistersExpectedFunctions()
  {
    var tools = _pluginFactory.ResolveTools([
      Tools.ResearchBet.GetMatchBasicInfo(7),
      Tools.ResearchBet.GetMatchEvents(7),
      Tools.ResearchBet.PlaceBetSlip(7),
    ]);

    tools.Should().HaveCount(3);
    ToolNames(tools).Should().BeEquivalentTo(["GetMatchBasicInfo", "GetMatchEvents", "PlaceBetSlip"]);
  }

  [Fact]
  public void BettingPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = _pluginFactory.ResolveTools([
      Tools.Betting.GetAvailableMatches,
      Tools.Betting.GetCurrentOdds,
      Tools.Betting.GetMatchAnalysis,
      Tools.Betting.PlaceBetSlip,
      Tools.Betting.GetBetSlips,
    ]);

    tools.Should().HaveCount(5);
    ToolNames(tools).Should().Contain(["GetAvailableMatches", "PlaceBetSlip", "GetBetSlips"]);
  }

  [Fact]
  public void BettingXPostStepTools_RegistersCreateXPost()
  {
    var tools = _pluginFactory.ResolveTools([Tools.SocialMedia.CreateXPost]);

    tools.Should().ContainSingle();
    ToolNames(tools).Should().ContainSingle("CreateXPost");
  }

  [Fact]
  public void ReflectionStepTools_RegistersExpectedFunctions()
  {
    var tools = _pluginFactory.ResolveTools([
      Tools.Betting.GetBetSlipsAwaitingReflection,
      Tools.Match.GetMatchResearchText,
    ]);

    tools.Should().HaveCount(2);
    ToolNames(tools).Should().Contain(["GetBetSlipsAwaitingReflectionAsync", "GetMatchResearchTextAsync"]);
  }

  [Fact]
  public void MemoryCleanupStepTools_RegistersNoPluginTools()
  {
    var tools = _pluginFactory.ResolveTools([]);

    tools.Should().BeEmpty();
  }

  [Fact]
  public void InternetResearchStepTools_RegistersExpectedFunctions()
  {
    var tools = _pluginFactory.ResolveTools([
      Tools.Match.GetUpcomingMatches,
    ]);

    tools.Should().HaveCount(1);
    ToolNames(tools).Should().Contain(["GetAvailableMatchesAsync"]);
  }

  private static IEnumerable<string> ToolNames(IReadOnlyList<AITool> tools) =>
    tools.Cast<AIFunction>().Select(t => t.Name);
}
