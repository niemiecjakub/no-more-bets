using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace NoMoreBets.Infrastructure.Scraping.BrowserAutomation;

/// <summary>
/// Fetches page HTML using Playwright with WaitUntilState.Load (avoids timeout on sites that never reach networkidle).
/// Throws <see cref="PermanentScraperException"/> for HTTP 403, 404, 410.
/// Supports both simple fetch and interactive fetch (clicks before capture) for consent/modals.
/// Uses a shared persistent browser and context pool (max 3). By default blocks image, media, font, stylesheet. Navigation timeout capped at 35s.
/// </summary>
public class PlaywrightPageFetcher
{
  private const int MaxNavigationTimeoutMs = 35_000;

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
               "Chrome/131.0.0.0 Safari/537.36",
    ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
    Locale = "pl-PL",
    TimezoneId = "Europe/Warsaw",
    ScreenSize = new ScreenSize { Width = 1366, Height = 768 }
  };

  private static readonly HashSet<string> AlwaysBlockedResourceTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "image", "media", "font"
  };

  /// <param name="blockStylesheets">When false, stylesheets are allowed (some CSR sites need CSS before the table mounts).</param>
  private static async Task ApplyResourceBlockingAsync(IPage page, bool blockStylesheets)
  {
    await page.RouteAsync("**/*", async route =>
    {
      var resourceType = route.Request.ResourceType;
      if (AlwaysBlockedResourceTypes.Contains(resourceType))
      {
        await route.AbortAsync().ConfigureAwait(false);
        return;
      }

      if (blockStylesheets &&
          string.Equals(resourceType, "stylesheet", StringComparison.OrdinalIgnoreCase))
      {
        await route.AbortAsync().ConfigureAwait(false);
        return;
      }

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
        await ApplyResourceBlockingAsync(page, blockStylesheets: true).ConfigureAwait(false);

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
  /// <param name="url">URL to load.</param>
  /// <param name="steps">Interactions to run after load (e.g. consent clicks).</param>
  /// <param name="timeout">Navigation and wait timeout budget.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <param name="waitForSelectorBeforeContent">If set (and no function wait), waits for this selector to be attached before reading DOM.</param>
  /// <param name="waitForFunctionBeforeContent">If set, waits for this browser function to return true (preferred for fragile class names). Example: <c>() =&gt; document.querySelectorAll("div").length &gt; 0</c>.</param>
  /// <param name="blockStylesheets">When false, allow stylesheets during this session (FotMob table CSR).</param>
  public virtual async Task<string> GetHtmlAfterInteractionsAsync(
    string url,
    IReadOnlyList<InteractionStep> steps,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default,
    string? waitForSelectorBeforeContent = null,
    string? waitForFunctionBeforeContent = null,
    bool blockStylesheets = true)
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
        await ApplyResourceBlockingAsync(page, blockStylesheets).ConfigureAwait(false);

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

          var waitMs = Math.Max(5000, Math.Min(cappedTimeoutMs, 32_000));
          if (!string.IsNullOrWhiteSpace(waitForFunctionBeforeContent))
          {
            try
            {
              await page.WaitForFunctionAsync(waitForFunctionBeforeContent.Trim(), new PageWaitForFunctionOptions
              {
                Timeout = waitMs,
                PollingInterval = 250
              }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
              _logger.LogWarning(ex, "WaitForFunction failed on {Url}", url);
            }
          }
          else if (!string.IsNullOrWhiteSpace(waitForSelectorBeforeContent))
          {
            try
            {
              await page.WaitForSelectorAsync(waitForSelectorBeforeContent.Trim(), new PageWaitForSelectorOptions
              {
                Timeout = waitMs,
                State = WaitForSelectorState.Attached
              }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
              _logger.LogWarning(ex, "WaitForSelector failed for {Selector} on {Url}", waitForSelectorBeforeContent, url);
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
