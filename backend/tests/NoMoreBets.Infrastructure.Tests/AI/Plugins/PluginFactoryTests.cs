using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

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
      .AddSingleton(sp => new InternetSearchPlugin(sp.GetRequiredService<ISearchService>()))
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
    var sut = CreateSut();

    var plugin = sut.CreateBettingPlugin();

    plugin.Should().BeOfType<BettingPlugin>();
  }

  [Fact]
  public void CreateInternetSearchPlugin_ReturnsInstance()
  {
    var sut = CreateSut();

    var plugin = sut.CreateInternetSearchPlugin();

    plugin.Should().BeOfType<InternetSearchPlugin>();
  }

  [Fact]
  public void CreateSocialMediaPlugin_ReturnsInstance()
  {
    var sut = CreateSut();

    var plugin = sut.CreateSocialMediaPlugin();

    plugin.Should().BeOfType<SocialMediaPlugin>();
  }

  [Fact]
  public void CreateResearchBetPlugin_ReturnsInstance()
  {
    var sut = CreateSut();

    var plugin = sut.CreateResearchBetPlugin(42);

    plugin.Should().BeOfType<ResearchBetPlugin>();
  }
}
