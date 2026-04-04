using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class PluginFactoryTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly ISearchService _searchService = Substitute.For<ISearchService>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();

  private PluginFactory CreateSut()
  {
    _unitOfWork.Matches.Returns(_matchRepository);
    var sp = new ServiceCollection()
      .AddSingleton(_unitOfWork)
      .AddSingleton(_mediator)
      .AddSingleton(_searchService)
      .BuildServiceProvider();
    return new PluginFactory(sp);
  }

  [Fact]
  public async Task CreateMatchPluginAsync_WhenMatchMissing_ThrowsArgumentException()
  {
    // Arrange
    _matchRepository.GetMatchByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Match?)null);
    var sut = CreateSut();

    // Act
    var act = async () => await sut.CreateMatchPluginAsync(1, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Match 1 not found*");
  }

  [Fact]
  public async Task CreateMatchPluginAsync_WhenMatchExists_ReturnsMatchPlugin()
  {
    // Arrange
    var match = new Match { Id = 5 };
    _matchRepository.GetMatchByIdAsync(5, Arg.Any<CancellationToken>()).Returns(match);
    var sut = CreateSut();

    // Act
    var plugin = await sut.CreateMatchPluginAsync(5, CancellationToken.None);

    // Assert
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
