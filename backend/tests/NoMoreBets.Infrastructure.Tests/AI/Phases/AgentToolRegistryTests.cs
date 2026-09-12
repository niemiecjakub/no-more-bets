using FluentAssertions;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Phases.Betting;
using NoMoreBets.Infrastructure.AI.Phases.DailySlip;
using NoMoreBets.Infrastructure.AI.Phases.InternetResearch;
using NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;
using NoMoreBets.Infrastructure.AI.Phases.Reflection;
using NoMoreBets.Infrastructure.AI.Phases.Research;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Infrastructure.Tests.AI.Phases;

public class AgentToolRegistryTests
{
  private readonly IServiceProvider _serviceProvider;

  public AgentToolRegistryTests()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var matches = Substitute.For<IMatchRepository>();
    var betting = Substitute.For<IBettingRepository>();
    var bankroll = Substitute.For<IBankrollRepository>();
    var mediator = Substitute.For<IMediator>();
    var xApiService = Substitute.For<IXApiService>();
    var agentSessionContext = new AgentSessionContext();

    _serviceProvider = new ServiceCollection()
      .AddSingleton(unitOfWork)
      .AddSingleton(matches)
      .AddSingleton(betting)
      .AddSingleton(bankroll)
      .AddSingleton(mediator)
      .AddSingleton(xApiService)
      .AddSingleton(agentSessionContext)
      .AddScoped<MatchTool>()
      .AddScoped<BettingTool>()
      .AddScoped<DailySlipTool>()
      .AddScoped<SocialMediaTool>()
      .BuildServiceProvider();
  }

  [Fact]
  public void ResearchPrimaryStepTools_RegistersExpectedFunctions()
  {
    var match = new Match
    {
      Id = 1,
      HomeClub = new ClubEntity { Name = "H" },
      AwayClub = new ClubEntity { Name = "A" },
      MatchDate = DateTime.UtcNow,
    };
    var tools = new ResearchExecuteStep(match).GetTools(_serviceProvider);

    tools.Should().HaveCount(9);
    ToolNames(tools).Should().Contain(["match_getLineups"]);
  }

  [Fact]
  public void ResearchPaperBetStepTools_RegistersExpectedFunctions()
  {
    var tools = new PaperBetFollowUpStep(7).GetTools(_serviceProvider);

    tools.Should().HaveCount(3);
    ToolNames(tools).Should().BeEquivalentTo(["researchbet_getMatchBasicInfo", "researchbet_getMatchEvents", "researchbet_placeBetSlip"]);
  }

  [Fact]
  public void DailySlipPlaceTools_IncludesMatchHelpers()
  {
    var tools = new DailySlipExecuteStep().GetTools(_serviceProvider);

    tools.Should().HaveCount(3);
    ToolNames(tools).Should().Contain(
    [
      "match_getClubRollingPerformance",
      "match_getLeagueTable",
      "match_getGroupTable",
    ]);
  }

  [Fact]
  public void BettingPrimaryStepTools_RegistersNoRunOptionTools()
  {
    var tools = new BettingExecuteStep().GetTools(_serviceProvider);
    tools.Should().BeEmpty();
  }

  [Fact]
  public void BettingXPostStepTools_RegistersCreateXPost()
  {
    var tools = new XPostFollowUpStep().GetTools(_serviceProvider);

    tools.Should().ContainSingle();
    ToolNames(tools).Should().ContainSingle("socialmedia_createXPost");
  }

  [Fact]
  public void ReflectionStepTools_RegistersExpectedFunctions()
  {
    var tools = new ReflectionExecuteStep().GetTools(_serviceProvider);

    tools.Should().HaveCount(2);
    ToolNames(tools).Should().Contain(["betting_getBetSlipsAwaitingReflectionAsync", "match_getMatchResearchTextAsync"]);
  }

  [Fact]
  public void MemoryCleanupStepTools_RegistersNoPluginTools()
  {
    var tools = new MemoryCleanupExecuteStep().GetTools(_serviceProvider);
    tools.Should().BeEmpty();
  }

  [Fact]
  public void InternetResearchStepTools_RegistersExpectedFunctions()
  {
    var tools = new InternetResearchExecuteStep().GetTools(_serviceProvider);

    tools.Should().HaveCount(1);
    ToolNames(tools).Should().Contain(["match_getAvailableMatchesAsync"]);
  }

  private static IEnumerable<string> ToolNames(IReadOnlyList<AITool> tools) =>
    tools.Cast<AIFunction>().Select(t => t.Name);
}
