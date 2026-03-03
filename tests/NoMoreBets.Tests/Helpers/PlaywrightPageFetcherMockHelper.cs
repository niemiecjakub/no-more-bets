using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;

namespace NoMoreBets.Tests.Helpers;

/// <summary>
/// Creates <see cref="Mock{PlaywrightPageFetcher}"/> with constructor dependencies required by <see cref="PlaywrightPageFetcher"/>.
/// Use when tests need to instantiate or mock PlaywrightPageFetcher (e.g. for scrapers that take it as a dependency).
/// </summary>
public static class PlaywrightPageFetcherMockHelper
{
  public static Mock<PlaywrightPageFetcher> CreateMock()
  {
    var browserServiceMock = new Mock<PlaywrightBrowserService>(NullLogger<PlaywrightBrowserService>.Instance);
    var proxyOptions = Options.Create(new ProxyOptions());
    return new Mock<PlaywrightPageFetcher>(
      NullLogger<PlaywrightPageFetcher>.Instance,
      browserServiceMock.Object,
      proxyOptions);
  }
}
