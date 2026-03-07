using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;

namespace NoMoreBets.Infrastructure.Tests.Helpers;

/// <summary>
/// Creates a substitute for <see cref="PlaywrightPageFetcher"/> with constructor dependencies.
/// Use when tests need to mock PlaywrightPageFetcher (e.g. for scrapers that take it as a dependency).
/// </summary>
public static class PlaywrightPageFetcherMockHelper
{
  public static PlaywrightPageFetcher CreateMock()
  {
    var browserService = Substitute.For<PlaywrightBrowserService>(NullLogger<PlaywrightBrowserService>.Instance);
    var proxyOptions = Options.Create(new ProxyOptions());
    return Substitute.For<PlaywrightPageFetcher>(
      NullLogger<PlaywrightPageFetcher>.Instance,
      browserService,
      proxyOptions);
  }
}
