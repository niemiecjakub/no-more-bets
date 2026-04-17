using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Search;
using NoMoreBets.Application.Search.SearchLlmContext;
using NoMoreBets.Application.Search.SearchNews;
using NoMoreBets.Infrastructure.AI.Plugins;
using AppLlmItem = NoMoreBets.Application.Search.SearchLlmContext.SearchLlmContextItemDto;
using AppNewsArticle = NoMoreBets.Application.Search.SearchNews.SearchNewsArticleDto;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class InternetSearchPluginTests
{
  private readonly ISearchService _searchService = Substitute.For<ISearchService>();
  private readonly InternetSearchPlugin _sut;

  public InternetSearchPluginTests()
  {
    _sut = new InternetSearchPlugin(_searchService);
  }

  [Fact]
  public async Task SearchNewsAsync_OrdersByPublishedAtDescending_AndMergesSnippets()
  {
    // Arrange
    var older = DateTimeOffset.Parse("2026-04-01T10:00:00Z");
    var newer = DateTimeOffset.Parse("2026-04-02T10:00:00Z");
    var dto = new SearchNewsResultDto
    {
      Items =
      [
        new AppNewsArticle
        {
          Title = "Old",
          Source = "s1",
          PublishedAt = older,
          Snippet = "a",
          ExtraSnippets = ["b"]
        },
        new AppNewsArticle
        {
          Title = "New",
          Source = "s2",
          PublishedAt = newer,
          Snippet = "x",
          ExtraSnippets = []
        }
      ]
    };
    _searchService.SearchNewsAsync(Arg.Any<string>(), Arg.Any<SearchNewsOptions>(), Arg.Any<CancellationToken>())
      .Returns(dto);

    // Act
    var result = await _sut.SearchNewsAsync("topic", cancellationToken: CancellationToken.None);

    // Assert
    result.Should().HaveCount(2);
    result[0].Title.Should().Be("New");
    result[1].Title.Should().Be("Old");
    result[1].Snippets.Should().Equal("a", "b");
    result[0].Snippets.Should().Equal("x");
    await _searchService.Received(1).SearchNewsAsync(
      "topic",
      Arg.Is<SearchNewsOptions>(o => o.Count == 3 && o.Freshness == null && o.Country == "GB" && o.ExtraSnippets),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetWebGroundingAsync_MapsItem()
  {
    // Arrange
    var dto = new SearchLlmContextResultDto
    {
      Items =
      [
        new AppLlmItem
        {
          Snippets = ["s1"],
          Title = "T",
          Hostname = "h.test",
          Age = "1d"
        }
      ]
    };
    _searchService.SearchLlmContextAsync(Arg.Any<string>(), Arg.Any<SearchLlmContextOptions>(), Arg.Any<CancellationToken>())
      .Returns(dto);

    // Act
    var result = await _sut.GetWebGroundingAsync("why", cancellationToken: CancellationToken.None);

    // Assert
    result.Title.Should().Be("T");
    result.Hostname.Should().Be("h.test");
    result.Age.Should().Be("1d");
    result.Snippets.Should().Equal("s1");
    await _searchService.Received(1).SearchLlmContextAsync(
      "why",
      Arg.Is<SearchLlmContextOptions>(o => o.Count == 1 && o.Freshness == null),
      Arg.Any<CancellationToken>());
  }
}
