using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using NoMoreBets.Infrastructure.Scraping;

namespace NoMoreBets.Infrastructure.Scraping.Playwright;

/// <summary>
/// Fetches page HTML using Playwright with WaitUntilState.Load (avoids timeout on sites that never reach networkidle).
/// Throws <see cref="PermanentScraperException"/> for HTTP 403, 404, 410.
/// Supports both simple fetch and interactive fetch (clicks before capture) for consent/modals.
/// Uses a shared persistent browser and context pool (max 3). Blocks image, media, font, stylesheet. Navigation timeout capped at 20s.
/// </summary>
public class PlaywrightPageFetcher
{
  private const int MaxNavigationTimeoutMs = 20_000;

  private static readonly HashSet<string> BlockedResourceTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "image", "media", "font", "stylesheet"
  };

  private readonly ILogger<PlaywrightPageFetcher> _logger;
  private readonly PlaywrightBrowserService _browserService;
  private readonly ProxyOptions _options;

  public PlaywrightPageFetcher(
    ILogger<PlaywrightPageFetcher> logger,
    PlaywrightBrowserService browserService,
    IOptions<ProxyOptions> options)
  {
    _logger = logger;
    _browserService = browserService;
    _options = options.Value;
  }

  private Proxy? GetProxy()
  {
    if (!_options.IsValid())
      return null;

    return new Proxy
    {
      Server = _options.ProxyServer,
      Username = _options.ProxyUser,
      Password = _options.ProxyPassword
    };
  }

  private static BrowserNewContextOptions BuildContextOptions(Proxy? proxy) => new()
  {
    Proxy = proxy,
    IgnoreHTTPSErrors = true,
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
               "AppleWebKit/537.36 (KHTML, like Gecko) " +
               "Chrome/117.0.0.0 Safari/537.36",
    ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
    Locale = "pl-PL",
    TimezoneId = "Europe/Warsaw",
    ScreenSize = new ScreenSize { Width = 1366, Height = 768 }
  };

  private static async Task BlockUnwantedResourcesAsync(IPage page)
  {
    await page.RouteAsync("**/*", async route =>
    {
      var resourceType = route.Request.ResourceType;
      if (BlockedResourceTypes.Contains(resourceType))
        await route.AbortAsync().ConfigureAwait(false);
      else
        await route.ContinueAsync().ConfigureAwait(false);
    }).ConfigureAwait(false);
  }

  /// <summary>Navigates to the URL, waits for page load, and returns the HTML content.</summary>
  public virtual async Task<string> GetHtmlAsync(
    string url,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
  {
    var timeoutMs = timeout.HasValue
      ? (int)timeout.Value.TotalMilliseconds
      : (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
    var cappedTimeoutMs = Math.Min(timeoutMs, MaxNavigationTimeoutMs);

    return await _browserService.RunWithContextSlotAsync(async browser =>
    {
      var context = await browser.NewContextAsync(BuildContextOptions(GetProxy())).ConfigureAwait(false);
      try
      {
        var page = await context.NewPageAsync().ConfigureAwait(false);
        await BlockUnwantedResourcesAsync(page).ConfigureAwait(false);

        try
        {
          IResponse? response = null;
          try
          {
            response = await page.GotoAsync(url, new PageGotoOptions
            {
              WaitUntil = WaitUntilState.Load,
              Timeout = cappedTimeoutMs
            }).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Playwright navigation failed for {Url}", url);
            throw;
          }

          if (response is not null)
          {
            var status = response.Status;
            if (status is 403 or 404 or 410)
              throw new PermanentScraperException($"Permanent failure ({status}) for {url}", status);
          }

          var html = await page.ContentAsync().ConfigureAwait(false);
          return html ?? string.Empty;
        }
        finally
        {
          await page.CloseAsync().ConfigureAwait(false);
        }
      }
      finally
      {
        await context.DisposeAsync().ConfigureAwait(false);
      }
    }, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>Navigates to the URL, runs interaction steps, then returns the HTML content.</summary>
  public virtual async Task<string> GetHtmlAfterInteractionsAsync(
    string url,
    IReadOnlyList<InteractionStep> steps,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
  {
    var timeoutMs = timeout.HasValue
      ? (int)timeout.Value.TotalMilliseconds
      : (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
    var cappedTimeoutMs = Math.Min(timeoutMs, MaxNavigationTimeoutMs);

    return await _browserService.RunWithContextSlotAsync(async browser =>
    {
      var context = await browser.NewContextAsync(BuildContextOptions(GetProxy())).ConfigureAwait(false);
      try
      {
        var page = await context.NewPageAsync().ConfigureAwait(false);
        await BlockUnwantedResourcesAsync(page).ConfigureAwait(false);

        try
        {
          IResponse? response = null;
          try
          {
            response = await page.GotoAsync(url, new PageGotoOptions
            {
              WaitUntil = WaitUntilState.Load,
              Timeout = cappedTimeoutMs
            }).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Playwright navigation failed for {Url}", url);
            throw;
          }

          if (response is not null)
          {
            var status = response.Status;
            if (status is 403 or 404 or 410)
              throw new PermanentScraperException($"Permanent failure ({status}) for {url}", status);
          }

          await Task.Delay(Random.Shared.Next(1500, 3000), cancellationToken).ConfigureAwait(false);

          foreach (var step in steps)
          {
            cancellationToken.ThrowIfCancellationRequested();
            if (step.Action != InteractionAction.Click)
              continue;

            var locators = await page.Locator(step.Selector).AllAsync().ConfigureAwait(false);
            foreach (var locator in locators)
            {
              try
              {
                await locator.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                await Task.Delay(Random.Shared.Next(200, 600), cancellationToken).ConfigureAwait(false);
                var box = await locator.BoundingBoxAsync();
                if (box != null)
                {
                  var x = box.X + box.Width / 2;
                  var y = box.Y + box.Height / 2;
                  await page.Mouse.MoveAsync(x, y, new MouseMoveOptions { Steps = Random.Shared.Next(5, 15) });
                  await Task.Delay(Random.Shared.Next(100, 300), cancellationToken).ConfigureAwait(false);
                  await locator.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                  await Task.Delay(Math.Max(step.DelayAfterMs, Random.Shared.Next(300, 800)), cancellationToken).ConfigureAwait(false);
                }
              }
              catch (Exception ex)
              {
                _logger.LogDebug(ex, "Interaction step failed for selector {Selector}", step.Selector);
              }
            }
          }

          await Task.Delay(Random.Shared.Next(400, 1000), cancellationToken).ConfigureAwait(false);
          var html = await page.ContentAsync().ConfigureAwait(false);
          return html ?? string.Empty;
        }
        finally
        {
          await page.CloseAsync().ConfigureAwait(false);
        }
      }
      finally
      {
        await context.DisposeAsync().ConfigureAwait(false);
      }
    }, cancellationToken).ConfigureAwait(false);
  }
}
