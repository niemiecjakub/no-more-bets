using FluentAssertions;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.Tests.AI.Phases;

public class AgentToolRegistryTests
{
  private readonly IServiceProvider _serviceProvider;

  public AgentToolRegistryTests()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var mediator = Substitute.For<IMediator>();
    var xApiService = Substitute.For<IXApiService>();
    var agentSessionContext = new AgentSessionContext();

    _serviceProvider = new ServiceCollection()
      .AddSingleton(unitOfWork)
      .AddSingleton(mediator)
      .AddSingleton(xApiService)
      .AddSingleton(agentSessionContext)
      .AddScoped<MatchTool>()
      .AddScoped<BettingTool>()
      .AddScoped<SocialMediaTool>()
      .BuildServiceProvider();
  }

  [Fact]
  public void ResearchPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = _serviceProvider.ResolveTools([
      ToolRegistry.Match.GetLineups,
      ToolRegistry.Match.GetInjuries,
      ToolRegistry.Match.GetHead2HeadStats,
      ToolRegistry.Match.GetClubDailySummary,
      ToolRegistry.Match.GetClubRecentGames,
      ToolRegistry.Match.GetClubLeagueStatistics,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetMatchBettingOddsHistory,
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.SaveMatchAnalysis,
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
    var tools = _serviceProvider.ResolveTools([
      ToolRegistry.ResearchBet.GetMatchBasicInfo(7),
      ToolRegistry.ResearchBet.GetMatchEvents(7),
      ToolRegistry.ResearchBet.PlaceBetSlip(7),
    ]);

    tools.Should().HaveCount(3);
    ToolNames(tools).Should().BeEquivalentTo(["GetMatchBasicInfo", "GetMatchEvents", "PlaceBetSlip"]);
  }

  [Fact]
  public void BettingPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = _serviceProvider.ResolveTools([
      ToolRegistry.Betting.GetAvailableMatches,
      ToolRegistry.Betting.GetCurrentOdds,
      ToolRegistry.Betting.GetMatchAnalysis,
      ToolRegistry.Betting.PlaceBetSlip,
      ToolRegistry.Betting.GetBetSlips,
    ]);

    tools.Should().HaveCount(5);
    ToolNames(tools).Should().Contain(["GetAvailableMatches", "PlaceBetSlip", "GetBetSlips"]);
  }

  [Fact]
  public void BettingXPostStepTools_RegistersCreateXPost()
  {
    var tools = _serviceProvider.ResolveTools([ToolRegistry.SocialMedia.CreateXPost]);

    tools.Should().ContainSingle();
    ToolNames(tools).Should().ContainSingle("CreateXPost");
  }

  [Fact]
  public void ReflectionStepTools_RegistersExpectedFunctions()
  {
    var tools = _serviceProvider.ResolveTools([
      ToolRegistry.Betting.GetBetSlipsAwaitingReflection,
      ToolRegistry.Match.GetMatchResearchText,
    ]);

    tools.Should().HaveCount(2);
    ToolNames(tools).Should().Contain(["GetBetSlipsAwaitingReflectionAsync", "GetMatchResearchTextAsync"]);
  }

  [Fact]
  public void MemoryCleanupStepTools_RegistersNoPluginTools()
  {
    var tools = _serviceProvider.ResolveTools([]);

    tools.Should().BeEmpty();
  }

  [Fact]
  public void InternetResearchStepTools_RegistersExpectedFunctions()
  {
    var tools = _serviceProvider.ResolveTools([
      ToolRegistry.Match.GetUpcomingMatches,
    ]);

    tools.Should().HaveCount(1);
    ToolNames(tools).Should().Contain(["GetAvailableMatchesAsync"]);
  }

  private static IEnumerable<string> ToolNames(IReadOnlyList<AITool> tools) =>
    tools.Cast<AIFunction>().Select(t => t.Name);
}
