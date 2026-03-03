using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Infrastructure.Scraping;

public class BaseScraperTests
{
    /// <summary>
    /// Test-only scraper that exposes protected GetPageHtmlAsync for unit tests.
    /// </summary>
    private sealed class TestableScraper : BaseScraper
    {
        public TestableScraper(
            PlaywrightPageFetcher pageFetcher,
            IOptions<BaseScraperOptions> options,
            ILogger logger)
            : base(pageFetcher, options, logger)
        {
        }

        public Task<string> FetchAsync(string url, CancellationToken cancellationToken = default) =>
            GetPageHtmlAsync(url, cancellationToken);
    }

    private static BaseScraperOptions DefaultOptions() => new()
    {
        DelaySeconds = 0,
        RetryCount = 3,
        RetryDelaySeconds = 0.01,
        TimeoutSeconds = 15
    };

    private static TestableScraper CreateSut(
        PlaywrightPageFetcher pageFetcher,
        BaseScraperOptions? options = null)
    {
        var opts = Options.Create(options ?? DefaultOptions());
        var logger = NullLogger<TestableScraper>.Instance;
        return new TestableScraper(pageFetcher, opts, logger);
    }

    [Fact]
    public async Task GetPageHtmlAsync_CallsFetcher_ReturnsContent()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html><body>Fetched</body></html>";
        var fetcherMock = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(fetchedHtml);
        var sut = CreateSut(pageFetcher: fetcherMock.Object);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsPermanentScraperException_DoesNotRetry()
    {
        var url = "https://example.com/404";
        var fetcherMock = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromException<string>(new PermanentScraperException("Permanent failure (404)", 404)));
        var sut = CreateSut(pageFetcher: fetcherMock.Object);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<PermanentScraperException>().WithMessage("*404*");
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsTransient_RetriesAndSucceeds()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html>OK</html>";
        var callCount = 0;
        var fetcherMock = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcherMock.Setup(f => f.GetHtmlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                    return Task.FromException<string>(new InvalidOperationException("Network error"));
                return Task.FromResult(fetchedHtml);
            });
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(pageFetcher: fetcherMock.Object, opts);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenAllRetriesFail_ThrowsWithMessage()
    {
        var url = "https://example.com";
        var fetcherMock = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromException<string>(new InvalidOperationException("Transient failure")));
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(pageFetcher: fetcherMock.Object, opts);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Failed to fetch {url} after 2 attempts*");
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenRateLimitActive_WaitsBeforeSecondFetch()
    {
        var url = "https://example.com";
        var callCount = 0;
        var fetcherMock = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(Interlocked.Increment(ref callCount) == 1 ? "<html>1</html>" : "<html>2</html>"));
        var opts = DefaultOptions() with { DelaySeconds = 0.1, RetryDelaySeconds = 0 };
        var sut = CreateSut(pageFetcher: fetcherMock.Object, opts);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var r1 = await sut.FetchAsync(url);
        await Task.Delay(150); // allow rate limiter window to renew so second fetch gets a permit
        var r2 = await sut.FetchAsync(url);
        sw.Stop();

        r1.Should().Be("<html>1</html>");
        r2.Should().Be("<html>2</html>");
        sw.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(0.1, "rate limit window should have passed before second fetch");
    }
}
