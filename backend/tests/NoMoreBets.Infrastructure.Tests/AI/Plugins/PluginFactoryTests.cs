using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class PluginFactoryTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly ISearchService _searchService = Substitute.For<ISearchService>();
  private readonly IXApiService _xApiService = Substitute.For<IXApiService>();
  private readonly AgentSessionContext _agentSessionContext = new();

  private PluginFactory CreateSut()
  {
    var sp = new ServiceCollection()
      .AddSingleton(_unitOfWork)
      .AddSingleton(_mediator)
      .AddSingleton(_searchService)
      .AddSingleton(_xApiService)
      .AddSingleton(_agentSessionContext)
      .AddSingleton(sp => new MemoriesPlugin(sp.GetRequiredService<IUnitOfWork>()))
      .AddSingleton(sp => new InternetSearchPlugin(sp.GetRequiredService<ISearchService>()))
      .AddSingleton(sp => new BankrollPlugin(sp.GetRequiredService<IUnitOfWork>(), sp.GetRequiredService<IMediator>()))
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
  public void CreateInternetSearchPlugin_ReturnsInstance()
  {
    // Arrange
    var sut = CreateSut();

    // Act
    var plugin = sut.CreateInternetSearchPlugin();

    // Assert
    plugin.Should().BeOfType<InternetSearchPlugin>();
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

  [Fact]
  public void CreateSocialMediaPlugin_ReturnsInstance()
  {
    var sut = CreateSut();

    var plugin = sut.CreateSocialMediaPlugin();

    plugin.Should().BeOfType<SocialMediaPlugin>();
  }

  [Fact]
  public void CreateAgentMemoryMaintenancePlugin_ReturnsInstance()
  {
    var sut = CreateSut();

    var plugin = sut.CreateAgentMemoryMaintenancePlugin();

    plugin.Should().BeOfType<AgentMemoryMaintenancePlugin>();
  }
}
