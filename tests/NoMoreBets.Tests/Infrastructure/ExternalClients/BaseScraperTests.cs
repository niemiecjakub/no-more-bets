using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.Core;
using NoMoreBets.Infrastructure.ExternalClients;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Tests.Infrastructure.ExternalClients;

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
            IOptions<BaseScraperOptions> options,
            ILogger logger)
            : base(cache, fetcher, options, logger)
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
        BaseScraperOptions? options = null)
    {
        var opts = Options.Create(options ?? DefaultOptions());
        var logger = NullLogger<TestableScraper>.Instance;
        return new TestableScraper(cache, fetcher, opts, logger);
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenCacheHit_ReturnsCachedHtml_WithoutCallingFetcher()
    {
        var url = "https://example.com";
        var cachedHtml = "<html><body>Cached</body></html>";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns(cachedHtml);
        var fetcher = Substitute.For<IPageFetcher>();
        var sut = CreateSut(cache, fetcher);

        var result = await sut.FetchAsync(url);

        result.Should().Be(cachedHtml);
        await fetcher.DidNotReceive().GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenCacheMiss_CallsFetcher_AndSavesToCache()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html><body>Fetched</body></html>";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns((string?)null);
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(fetchedHtml);
        var sut = CreateSut(cache, fetcher);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        await fetcher.Received(1).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
        await cache.Received(1).SaveAsync(url, fetchedHtml, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsPermanentScraperException_DoesNotRetry()
    {
        var url = "https://example.com/404";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns((string?)null);
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns((Func<CallInfo, Task<string>>)(_ => Task.FromException<string>(new PermanentScraperException("Permanent failure (404)", 404))));
        var sut = CreateSut(cache, fetcher);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<PermanentScraperException>().WithMessage("*404*");
        await fetcher.Received(1).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenFetcherThrowsTransient_RetriesAndSucceeds()
    {
        var url = "https://example.com";
        var fetchedHtml = "<html>OK</html>";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns((string?)null);
        var callCount = 0;
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns((Func<CallInfo, Task<string>>)(_ =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                    return Task.FromException<string>(new InvalidOperationException("Network error"));
                return Task.FromResult(fetchedHtml);
            }));
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(cache, fetcher, opts);

        var result = await sut.FetchAsync(url);

        result.Should().Be(fetchedHtml);
        await fetcher.Received(2).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenAllRetriesFail_ThrowsWithMessage()
    {
        var url = "https://example.com";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns((string?)null);
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns((Func<CallInfo, Task<string>>)(_ => Task.FromException<string>(new InvalidOperationException("Transient failure"))));
        var opts = DefaultOptions() with { RetryCount = 2, RetryDelaySeconds = 0.01 };
        var sut = CreateSut(cache, fetcher, opts);

        var act = () => sut.FetchAsync(url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Failed to fetch {url} after 2 attempts*");
        await fetcher.Received(2).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearCacheAsync_DelegatesToCache()
    {
        var url = "https://example.com";
        var cache = Substitute.For<IHtmlCache>();
        cache.ClearAsync(url, Arg.Any<CancellationToken>()).Returns(1);
        var fetcher = Substitute.For<IPageFetcher>();
        var sut = CreateSut(cache, fetcher);

        var result = await sut.ClearCacheAsync(url);

        result.Should().Be(1);
        await cache.Received(1).ClearAsync(url, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPageHtmlAsync_WhenRateLimitActive_WaitsBeforeSecondFetch()
    {
        var url = "https://example.com";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns((string?)null);
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns("<html>1</html>", "<html>2</html>");
        var opts = DefaultOptions() with { DelaySeconds = 0.1, RetryDelaySeconds = 0 };
        var sut = CreateSut(cache, fetcher, opts);

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
