using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Tests.Infrastructure.Scraping;

public class BaseScraperTests
{
    /// <summary>
    /// Test-only scraper that exposes protected GetPageHtmlAsync for unit tests.
    /// </summary>
    private sealed class TestableScraper : BaseScraper
    {
        public TestableScraper(
            IHtmlCache cache,
            IPageFetcher fetcher,
            IInteractivePageFetcher interactiveFetcher,
            IOptions<BaseScraperOptions> options,
            ILogger logger)
            : base(cache, fetcher, interactiveFetcher, options, logger)
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
        IHtmlCache cache,
        IPageFetcher fetcher,
        BaseScraperOptions? options = null,
        IInteractivePageFetcher? interactiveFetcher = null)
    {
        var opts = Options.Create(options ?? DefaultOptions());
        var logger = NullLogger<TestableScraper>.Instance;
        var interactive = interactiveFetcher ?? new Mock<IInteractivePageFetcher>().Object;
        return new TestableScraper(cache, fetcher, interactive, opts, logger);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenCacheHit_ReturnsCachedHtml_WithoutCallingFetcher()
    {
        var url = "https://example.com";
        var cachedHtml = "<html><body>Cached</body></html>";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync(cachedHtml);
        var fetcherMock = new Mock<IPageFetcher>();
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object);

        var result = await sut.FetchAsync(url);

        result.Should().Be(cachedHtml);
        fetcherMock.Verify(f => f.GetHtmlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenCacheMiss_CallsFetcher_AndSavesToCache()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html><body>Fetched</body></html>";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var fetcherMock = new Mock<IPageFetcher>();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(fetchedHtml);
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(c => c.SaveAsync(url, fetchedHtml, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsPermanentScraperException_DoesNotRetry()
    {
        var url = "https://example.com/404";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var fetcherMock = new Mock<IPageFetcher>();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromException<string>(new PermanentScraperException("Permanent failure (404)", 404)));
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<PermanentScraperException>().WithMessage("*404*");
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsTransient_RetriesAndSucceeds()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html>OK</html>";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var callCount = 0;
        var fetcherMock = new Mock<IPageFetcher>();
        fetcherMock.Setup(f => f.GetHtmlAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                    return Task.FromException<string>(new InvalidOperationException("Network error"));
                return Task.FromResult(fetchedHtml);
            });
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object, opts);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenAllRetriesFail_ThrowsWithMessage()
    {
        var url = "https://example.com";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var fetcherMock = new Mock<IPageFetcher>();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromException<string>(new InvalidOperationException("Transient failure")));
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object, opts);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Failed to fetch {url} after 2 attempts*");
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ClearCacheAsync_DelegatesToCache()
    {
        var url = "https://example.com";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.ClearAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var fetcherMock = new Mock<IPageFetcher>();
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object);

        var result = await sut.ClearCacheAsync(url);

        result.Should().Be(1);
        cacheMock.Verify(c => c.ClearAsync(url, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenRateLimitActive_WaitsBeforeSecondFetch()
    {
        var url = "https://example.com";
        var cacheMock = new Mock<IHtmlCache>();
        cacheMock.Setup(c => c.LoadAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var callCount = 0;
        var fetcherMock = new Mock<IPageFetcher>();
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(Interlocked.Increment(ref callCount) == 1 ? "<html>1</html>" : "<html>2</html>"));
        var opts = DefaultOptions() with { DelaySeconds = 0.1, RetryDelaySeconds = 0 };
        var sut = CreateSut(cacheMock.Object, fetcherMock.Object, opts);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var t1 = sut.FetchAsync(url);
        var t2 = sut.FetchAsync(url);
        await Task.WhenAll(t1, t2);
        sw.Stop();

        (await t1).Should().Be("<html>1</html>");
        (await t2).Should().Be("<html>2</html>");
        sw.Elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(0.1, "rate limit should delay the second fetch");
    }
}
