using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.DailySlip;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.Tests.AI.Providers.DailySlip;

public class DailySlipProviderTests
{
  private readonly DailySlipTool _dailySlipTool;
  private readonly BettingTool _bettingTool;

  public DailySlipProviderTests()
  {
    var unitOfWork = Substitute.For<IUnitOfWork>();
    var mediator = Substitute.For<IMediator>();
    var agentSessionContext = new AgentSessionContext();
    _dailySlipTool = new DailySlipTool(unitOfWork, mediator, agentSessionContext);
    _bettingTool = new BettingTool(unitOfWork, mediator, agentSessionContext);
  }

  [Fact]
  public void GetToolNames_WhenPlacementIncluded_IncludesPlaceBetSlip()
  {
    // Arrange
    var provider = new DailySlipProvider(_dailySlipTool, _bettingTool, includePlacement: true);

    // Act
    var names = provider.GetToolNames();

    // Assert
    names.Should().Contain(AgentToolCatalog.DailySlip.PlaceBetSlip.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetAvailableMatches.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetCurrentOdds.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetCurrentOddsForMarket.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetMatchAnalysis.Name);
  }

  [Fact]
  public void GetToolNames_WhenPlacementExcluded_OmitsPlaceBetSlip()
  {
    // Arrange
    var provider = new DailySlipProvider(_dailySlipTool, _bettingTool, includePlacement: false);

    // Act
    var names = provider.GetToolNames();

    // Assert
    names.Should().NotContain(AgentToolCatalog.DailySlip.PlaceBetSlip.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetAvailableMatches.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetCurrentOdds.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetCurrentOddsForMarket.Name);
    names.Should().Contain(AgentToolCatalog.Betting.GetMatchAnalysis.Name);
  }
}
