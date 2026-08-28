using FluentAssertions;
using NoMoreBets.Application.AgentTools;

namespace NoMoreBets.Application.Tests.AgentTools;

public class AgentToolCatalogTests
{
  [Fact]
  public void All_HasUniqueNames()
  {
    // Arrange
    var names = AgentToolCatalog.All.Select(def => def.Name).ToList();

    // Act
    var distinctCount = names.Distinct(StringComparer.Ordinal).Count();

    // Assert
    distinctCount.Should().Be(names.Count);
  }

  [Fact]
  public void All_HasNonEmptyDisplayNames()
  {
    // Arrange
    // Act
    var invalid = AgentToolCatalog.All.Where(def => string.IsNullOrWhiteSpace(def.DisplayName)).ToList();

    // Assert
    invalid.Should().BeEmpty();
  }

  [Fact]
  public void All_ContainsKnownTools()
  {
    // Arrange
    var names = AgentToolCatalog.All.Select(def => def.Name).ToHashSet(StringComparer.Ordinal);

    // Act
    // Assert
    names.Should().Contain(AgentToolCatalog.Match.GetLineups.Name);
    names.Should().Contain(AgentToolCatalog.Bankroll.GetBalance.Name);
    names.Should().Contain(AgentToolCatalog.Todo.Add.Name);
    names.Should().Contain(AgentToolCatalog.WebSearch.SearchNews.Name);
    names.Should().Contain(AgentToolCatalog.DailySlip.PlaceBetSlip.Name);
  }

  [Fact]
  public void ResearchBetSessionTools_UseSessionMatchFlag()
  {
    // Arrange
    // Act
    // Assert
    AgentToolCatalog.ResearchBet.GetMatchBasicInfo.UsesSessionMatch.Should().BeTrue();
    AgentToolCatalog.ResearchBet.GetMatchEvents.UsesSessionMatch.Should().BeTrue();
    AgentToolCatalog.ResearchBet.PlaceBetSlip.UsesSessionMatch.Should().BeFalse();
  }
}
