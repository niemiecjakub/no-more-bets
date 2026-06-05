using FluentAssertions;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Phases.Betting;
using NoMoreBets.Infrastructure.AI.Phases.InternetResearch;
using NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;
using NoMoreBets.Infrastructure.AI.Phases.Reflection;
using NoMoreBets.Infrastructure.AI.Phases.Research;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Phases;

public class PhaseToolsTests
{
  private readonly IPluginFactory _pluginFactory;

  public PhaseToolsTests()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var mediator = Substitute.For<IMediator>();
    var searchService = Substitute.For<ISearchService>();
    var xApiService = Substitute.For<IXApiService>();
    var agentSessionContext = new AgentSessionContext();

    var sp = new ServiceCollection()
      .AddSingleton(unitOfWork)
      .AddSingleton(mediator)
      .AddSingleton(searchService)
      .AddSingleton(xApiService)
      .AddSingleton(agentSessionContext)
      .AddSingleton(sp => new MemoriesPlugin(sp.GetRequiredService<IUnitOfWork>()))
      .AddSingleton(sp => new InternetSearchPlugin(sp.GetRequiredService<ISearchService>()))
      .AddSingleton(sp => new BankrollPlugin(sp.GetRequiredService<IUnitOfWork>(), sp.GetRequiredService<IMediator>()))
      .BuildServiceProvider();

    _pluginFactory = new PluginFactory(sp);
  }

  [Fact]
  public void ResearchPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = ResearchPhaseTools.CreatePrimaryStepTools(_pluginFactory);

    tools.Should().HaveCount(19);
    ToolNames(tools).Should().Contain(
    [
      "GetLineups",
      "SaveMatchAnalysisAsync",
      "ReadMemoryAsync",
      "SearchNewsAsync",
      "GetCurrentBalance",
    ]);
  }

  [Fact]
  public void ResearchPaperBetStepTools_RegistersExpectedFunctions()
  {
    var tools = ResearchPhaseTools.CreatePaperBetStepTools(_pluginFactory, 7);

    tools.Should().HaveCount(3);
    ToolNames(tools).Should().BeEquivalentTo(["GetMatchBasicInfo", "GetMatchEvents", "PlaceBetSlip"]);
  }

  [Fact]
  public void BettingPrimaryStepTools_RegistersExpectedFunctions()
  {
    var tools = BettingPhaseTools.CreatePrimaryStepTools(_pluginFactory);

    tools.Should().HaveCount(14);
    ToolNames(tools).Should().Contain(["GetAvailableMatches", "PlaceBetSlip", "GetBetSlips"]);
  }

  [Fact]
  public void BettingXPostStepTools_RegistersCreateXPost()
  {
    var tools = BettingPhaseTools.CreateXPostStepTools(_pluginFactory);

    tools.Should().ContainSingle();
    ToolNames(tools).Should().ContainSingle("CreateXPost");
  }

  [Fact]
  public void ReflectionStepTools_RegistersExpectedFunctions()
  {
    var tools = ReflectionPhaseTools.CreateStepTools(_pluginFactory);

    tools.Should().HaveCount(9);
    ToolNames(tools).Should().Contain(["GetBetSlipsAwaitingReflectionAsync", "GetMatchResearchTextAsync"]);
  }

  [Fact]
  public void MemoryCleanupStepTools_RegistersDeleteMemory()
  {
    var tools = MemoryCleanupPhaseTools.CreateStepTools(_pluginFactory);

    tools.Should().HaveCount(10);
    ToolNames(tools).Should().Contain("DeleteMemoryAsync");
  }

  [Fact]
  public void InternetResearchStepTools_RegistersExpectedFunctions()
  {
    var tools = InternetResearchPhaseTools.CreateStepTools(_pluginFactory);

    tools.Should().HaveCount(10);
    ToolNames(tools).Should().Contain(["GetAvailableMatchesAsync", "SearchNewsAsync"]);
  }

  private static IEnumerable<string> ToolNames(IReadOnlyList<AITool> tools) =>
    tools.Cast<AIFunction>().Select(t => t.Name);
}
