using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace NoMoreBets.Infrastructure.Scraping.Playwright;

/// <summary>
/// Singleton that owns one Playwright instance and one Chromium process.
/// Limits concurrent context usage to 3 (context pool). Restarts browser only on crash/disconnect.
/// Uses a single init task so only one Chromium launch ever runs; no blocking inside locks.
/// </summary>
public class PlaywrightBrowserService : IDisposable
{
  private const int MaxContextSlots = 3;

  private readonly ILogger<PlaywrightBrowserService> _logger;
  private readonly SemaphoreSlim _contextSlots = new(MaxContextSlots, MaxContextSlots);
  private readonly object _browserLock = new();

  private Task<IBrowser>? _browserInitTask;
  private IPlaywright? _playwright;
  private IBrowser? _browser;

  public PlaywrightBrowserService(ILogger<PlaywrightBrowserService> logger)
  {
    _logger = logger;
  }

  public virtual async Task<T> RunWithContextSlotAsync<T>(
    Func<IBrowser, Task<T>> run,
    CancellationToken cancellationToken = default)
  {
    await _contextSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
    try
    {
      var task = GetBrowserAsync(cancellationToken);
      IBrowser browser;
      try
      {
        browser = await task.ConfigureAwait(false);
      }
      catch
      {
        lock (_browserLock)
        {
          if (_browserInitTask == task)
            _browserInitTask = null;
        }
        throw;
      }
      return await run(browser).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      if (IsBrowserGoneException(ex))
      {
        _logger.LogWarning(ex, "Browser disconnected or crashed; will recreate on next use");
        ClearBrowser();
      }
      throw;
    }
    finally
    {
      _contextSlots.Release();
    }
  }

  private Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
  {
    lock (_browserLock)
    {
      if (_browser != null)
        return Task.FromResult(_browser);

      if (_browserInitTask == null)
        _browserInitTask = CreateBrowserInternalAsync();

      return _browserInitTask;
    }
  }

  private async Task<IBrowser> CreateBrowserInternalAsync()
  {
    var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = true,
      Args = new[]
        {
            "--disable-blink-features=AutomationControlled",
            "--no-sandbox",
            "--disable-dev-shm-usage"
        }
    }).ConfigureAwait(false);

    lock (_browserLock)
    {
      // Check if the browser was already cleared
      if (_browser != null)
      {
        // Someone else already set _browser, close this one
        browser.CloseAsync().GetAwaiter().GetResult();
        playwright.Dispose();
        return _browser; // return the already-initialized browser
      }

      _playwright = playwright;
      _browser = browser;
      _logger.LogInformation("Playwright Chromium browser started (persistent, headless)");
      return _browser;
    }
  }

  private void ClearBrowser()
  {
    IBrowser? browserToClose = null;
    IPlaywright? playwrightToDispose = null;

    lock (_browserLock)
    {
      _browserInitTask = null;
      browserToClose = _browser;
      playwrightToDispose = _playwright;
      _browser = null;
      _playwright = null;
    }

    if (browserToClose != null)
    {
      try
      {
        browserToClose.CloseAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        _logger.LogDebug(ex, "Error closing browser during clear");
      }
    }

    if (playwrightToDispose != null)
    {
      try
      {
        playwrightToDispose.Dispose();
      }
      catch (Exception ex)
      {
        _logger.LogDebug(ex, "Error disposing Playwright during clear");
      }
    }
  }

  private static bool IsBrowserGoneException(Exception ex)
  {
    var message = ex.Message;
    return message.Contains("Browser closed", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("Target closed", StringComparison.OrdinalIgnoreCase) ||
           message.Contains("crashed", StringComparison.OrdinalIgnoreCase);
  }

  public void Dispose()
  {
    ClearBrowser();
    _contextSlots.Dispose();
  }
}
