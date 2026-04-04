using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class PluginFactoryTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly ISearchService _searchService = Substitute.For<ISearchService>();

  private PluginFactory CreateSut()
  {
    var sp = new ServiceCollection()
      .AddSingleton(_unitOfWork)
      .AddSingleton(_mediator)
      .AddSingleton(_searchService)
      .BuildServiceProvider();
    return new PluginFactory(sp);
  }

  [Fact]
  public void CreateMatchPlugin_ReturnsMatchPlugin()
  {
    var sut = CreateSut();

    var plugin = sut.CreateMatchPlugin();

    plugin.Should().BeOfType<MatchPlugin>();
  }

  [Fact]
  public void CreateBettingPlugin_ReturnsInstance()
  {
    // Arrange
    var sut = CreateSut();

    // Act
    var plugin = sut.CreateBettingPlugin();

    // Assert
    plugin.Should().BeOfType<BettingPlugin>();
  }

  [Fact]
  public void CreateSearchPlugin_ReturnsInstance()
  {
    // Arrange
    var sut = CreateSut();

    // Act
    var plugin = sut.CreateSearchPlugin();

    // Assert
    plugin.Should().BeOfType<SearchPlugin>();
  }

  [Fact]
  public void CreateMemoriesPlugin_ReturnsInstance()
  {
    // Arrange
    var sut = CreateSut();

    // Act
    var plugin = sut.CreateMemoriesPlugin();

    // Assert
    plugin.Should().BeOfType<MemoriesPlugin>();
  }

  [Fact]
  public void CreateBankrollPlugin_ReturnsInstance()
  {
    // Arrange
    var sut = CreateSut();

    // Act
    var plugin = sut.CreateBankrollPlugin();

    // Assert
    plugin.Should().BeOfType<BankrollPlugin>();
  }
}
