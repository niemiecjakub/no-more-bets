using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common.Dto.Matches;
using NoMoreBets.Application.Matches;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;

namespace NoMoreBets.Infrastructure.Scraping.External.Flashscore;

/// <summary>
/// Flashscore scraper for finished league results pages.
/// </summary>
public sealed partial class FlashscoreScraper : BaseScraper, IMatchResultsProvider
{
  private static readonly IReadOnlyDictionary<string, string> LeagueResultsUrls =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["ekstraklasa"] = "https://www.flashscore.com/football/poland/ekstraklasa/results/",
    };

  private readonly ILogger<FlashscoreScraper> _logger;

  public FlashscoreScraper(
    PlaywrightPageFetcher pageFetcher,
    IOptions<BaseScraperOptions> options,
    ILogger<FlashscoreScraper> logger)
    : base(pageFetcher, options, logger)
  {
    _logger = logger;
  }

  public async Task<IReadOnlyList<FinishedMatchResult>> GetFinishedResultsAsync(
    string leagueSlug,
    CancellationToken cancellationToken = default)
  {
    if (!LeagueResultsUrls.TryGetValue(leagueSlug, out var url))
    {
      _logger.LogWarning(
        "Flashscore results skipped: no URL mapping configured for league slug {LeagueSlug}",
        leagueSlug);
      return [];
    }

    var html = await GetPageHtmlAsync(
      url,
      cancellationToken,
      waitForSelectorBeforeContent: ".event__match").ConfigureAwait(false);

    var results = await ParseFinishedResultsAsync(html).ConfigureAwait(false);
    if (results.Count == 0)
    {
      _logger.LogWarning("Flashscore results returned no scored matches. Url: {Url}", url);
    }

    return results;
  }

  /// <summary>Parses finished match rows from a Flashscore results page HTML fragment.</summary>
  internal async Task<IReadOnlyList<FinishedMatchResult>> ParseFinishedResultsAsync(string html)
  {
    var config = Configuration.Default;
    var context = BrowsingContext.New(config);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var rows = doc.QuerySelectorAll(".event__match[data-event-row='true']");
    var results = new List<FinishedMatchResult>(rows.Length);
    foreach (var row in rows)
    {
      if (!TryParseRow(row, out var result))
        continue;

      results.Add(result);
    }

    return results;
  }

  private static bool TryParseRow(IElement row, out FinishedMatchResult result)
  {
    result = null!;

    var homeScoreText = row.QuerySelector(".event__score--home")?.TextContent?.Trim();
    var awayScoreText = row.QuerySelector(".event__score--away")?.TextContent?.Trim();
    if (!int.TryParse(homeScoreText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var homeGoals) ||
        !int.TryParse(awayScoreText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var awayGoals))
    {
      return false;
    }

    var homeTeam = row.QuerySelector(".event__homeParticipant [data-testid='wcl-scores-simple-text-01']")
      ?.TextContent?.Trim();
    var awayTeam = row.QuerySelector(".event__awayParticipant [data-testid='wcl-scores-simple-text-01']")
      ?.TextContent?.Trim();
    if (string.IsNullOrWhiteSpace(homeTeam) || string.IsNullOrWhiteSpace(awayTeam))
      return false;

    var stageTime = row.QuerySelector("[data-testid='wcl-stageTime']")?.TextContent?.Trim();
    if (!TryParseKickoff(stageTime, out var matchDate, out var kickoffTime))
      return false;

    var externalId = ExtractExternalId(row);
    if (string.IsNullOrWhiteSpace(externalId))
      return false;

    result = new FinishedMatchResult(
      externalId,
      homeTeam,
      awayTeam,
      matchDate,
      kickoffTime,
      homeGoals,
      awayGoals);
    return true;
  }

  private static string? ExtractExternalId(IElement row)
  {
    var id = row.Id;
    if (string.IsNullOrEmpty(id))
      return null;

    const string prefix = "g_1_";
    return id.StartsWith(prefix, StringComparison.Ordinal)
      ? id[prefix.Length..]
      : id;
  }

  internal static bool TryParseKickoff(string? text, out DateOnly matchDate, out TimeOnly? kickoffTime)
  {
    matchDate = default;
    kickoffTime = null;
    if (string.IsNullOrWhiteSpace(text))
      return false;

    var match = KickoffRegex().Match(text.Trim());
    if (!match.Success)
      return false;

    var day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);
    var month = int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture);
    var year = InferYear(month);

    try
    {
      matchDate = new DateOnly(year, month, day);
    }
    catch (ArgumentOutOfRangeException)
    {
      return false;
    }

    if (match.Groups["hour"].Success && match.Groups["minute"].Success)
    {
      var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
      var minute = int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture);
      if (hour is >= 0 and <= 23 && minute is >= 0 and <= 59)
        kickoffTime = new TimeOnly(hour, minute);
    }

    return true;
  }

  private static int InferYear(int month)
  {
    var now = DateTime.UtcNow;
    // Dec results in January → previous calendar year.
    if (month == 12 && now.Month == 1)
      return now.Year - 1;
    // Jan results in December → next calendar year (unlikely on results page, but keep symmetric).
    if (month == 1 && now.Month == 12)
      return now.Year + 1;
    return now.Year;
  }

  [GeneratedRegex(
    @"^(?<day>\d{1,2})\.(?<month>\d{1,2})\.(?:\s+(?<hour>\d{1,2}):(?<minute>\d{2}))?",
    RegexOptions.CultureInvariant | RegexOptions.Compiled)]
  private static partial Regex KickoffRegex();
}
