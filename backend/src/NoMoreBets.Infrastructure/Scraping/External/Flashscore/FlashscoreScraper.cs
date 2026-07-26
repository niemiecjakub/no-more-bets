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
/// Flashscore scraper for finished league results and match-summary incidents.
/// </summary>
public sealed partial class FlashscoreScraper : BaseScraper, IMatchResultsProvider, IMatchEventsProvider
{
  private const string FlashscoreOrigin = "https://www.flashscore.com";

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

  public async Task<IReadOnlyList<MatchEvent>> GetMatchEventsAsync(
    string matchDetailUrl,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(matchDetailUrl))
    {
      _logger.LogWarning("Flashscore match events skipped: match detail URL is empty");
      return [];
    }

    var html = await GetPageHtmlAsync(
      matchDetailUrl,
      cancellationToken,
      waitForSelectorBeforeContent: ".smv__participantRow").ConfigureAwait(false);

    var events = await ParseMatchEventsAsync(html).ConfigureAwait(false);
    if (events.Count == 0)
    {
      _logger.LogWarning(
        "Flashscore match events returned no incidents. Url: {MatchDetailUrl}",
        matchDetailUrl);
    }

    return events;
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

  /// <summary>Parses match-summary incidents from a Flashscore match detail HTML fragment.</summary>
  internal async Task<IReadOnlyList<MatchEvent>> ParseMatchEventsAsync(string html)
  {
    var config = Configuration.Default;
    var context = BrowsingContext.New(config);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var rows = doc.QuerySelectorAll(".smv__participantRow");
    var events = new List<MatchEvent>(rows.Length);
    foreach (var row in rows)
    {
      if (!TryParseIncidentRow(row, out var matchEvent))
        continue;

      events.Add(matchEvent);
    }

    return events;
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
      awayGoals,
      ExtractDetailUrl(row));
    return true;
  }

  private static bool TryParseIncidentRow(IElement row, out MatchEvent matchEvent)
  {
    matchEvent = null!;

    var team = row.ClassList.Contains("smv__homeParticipant")
      ? "home"
      : row.ClassList.Contains("smv__awayParticipant")
        ? "away"
        : null;
    if (team is null)
      return false;

    var incident = row.QuerySelector(".smv__incident");
    if (incident is null)
      return false;

    var minuteText = incident.QuerySelector(".smv__timeBox")?.TextContent?.Trim();
    if (string.IsNullOrWhiteSpace(minuteText))
      return false;

    var eventMinute = minuteText.TrimEnd('\'', '’').Trim();
    if (string.IsNullOrWhiteSpace(eventMinute))
      return false;

    var eventType = ClassifyIncident(incident);
    if (eventType is null)
      return false;

    if (eventType == "substitution")
    {
      var playerIn = TryParsePlayer(GetPrimaryPlayerLink(incident));
      var playerOut = TryParsePlayer(incident.QuerySelector(".smv__incidentSubOut a.smv__playerName"));
      if (playerIn is null && playerOut is null)
        return false;

      matchEvent = new MatchEvent
      {
        EventType = eventType,
        EventMinute = eventMinute,
        Team = team,
        PlayerIn = playerIn,
        PlayerOut = playerOut
      };
      return true;
    }

    var player = TryParsePlayer(GetPrimaryPlayerLink(incident));
    if (player is null)
      return false;

    matchEvent = new MatchEvent
    {
      EventType = eventType,
      EventMinute = eventMinute,
      Team = team,
      Player = player,
      AssistPlayer = TryParsePlayer(incident.QuerySelector(".smv__assist a"))
    };
    return true;
  }

  private static string? ClassifyIncident(IElement incident)
  {
    var icon = incident.QuerySelector(".smv__incidentIcon svg, .smv__incidentIconSub svg");
    if (icon is null)
      return null;

    var testId = icon.GetAttribute("data-testid");
    if (string.Equals(testId, "wcl-icon-incidents-substitution", StringComparison.Ordinal))
      return "substitution";
    if (string.Equals(testId, "wcl-icon-incidents-penalty-goal", StringComparison.Ordinal))
      return "penalty_goal";
    if (string.Equals(testId, "wcl-icon-incidents-red-card-second", StringComparison.Ordinal))
      return "yellow_red_card";
    if (string.Equals(testId, "wcl-icon-incidents-goal-soccer", StringComparison.Ordinal))
    {
      return icon.ClassList.Contains("footballOwnGoal-ico")
        ? "own_goal"
        : "goal";
    }

    if (icon.ClassList.Contains("yellowCard-ico"))
      return "yellow_card";
    if (icon.ClassList.Contains("redCard-ico"))
      return "red_card";

    return null;
  }

  private static IElement? GetPrimaryPlayerLink(IElement incident)
  {
    foreach (var link in incident.QuerySelectorAll("a.smv__playerName"))
    {
      if (link.Closest(".smv__incidentSubOut") is not null)
        continue;
      if (link.Closest(".smv__assist") is not null)
        continue;
      return link;
    }

    return null;
  }

  private static Player? TryParsePlayer(IElement? link)
  {
    if (link is null)
      return null;

    var name = link.TextContent?.Trim();
    if (string.IsNullOrWhiteSpace(name))
      return null;

    var href = link.GetAttribute("href");
    if (!TryExtractFlashscorePlayerId(href, out var flashscorePlayerId))
      return null;

    return new Player
    {
      Id = ToNegativeSoccerdataId(flashscorePlayerId),
      Name = name
    };
  }

  internal static bool TryExtractFlashscorePlayerId(string? href, out string playerId)
  {
    playerId = string.Empty;
    if (string.IsNullOrWhiteSpace(href))
      return false;

    var match = PlayerPathRegex().Match(href);
    if (!match.Success)
      return false;

    playerId = match.Groups["id"].Value;
    return !string.IsNullOrWhiteSpace(playerId);
  }

  /// <summary>
  /// Stable negative SoccerdataId from a Flashscore player path id.
  /// Real SoccerData ids are positive; negatives are reserved for Flashscore.
  /// </summary>
  internal static int ToNegativeSoccerdataId(string flashscorePlayerId)
  {
    // FNV-1a 32-bit — process-stable (unlike string.GetHashCode).
    unchecked
    {
      uint hash = 2166136261;
      foreach (var c in flashscorePlayerId)
        hash = (hash ^ c) * 16777619;

      return -(1 + (int)(hash & 0x7FFFFFFF));
    }
  }

  private static string? ExtractDetailUrl(IElement row)
  {
    var href = row.QuerySelector("a.eventRowLink")?.GetAttribute("href")?.Trim();
    if (string.IsNullOrWhiteSpace(href))
      return null;

    if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
      return absolute.ToString();

    if (Uri.TryCreate(new Uri(FlashscoreOrigin), href, out var combined))
      return combined.ToString();

    return null;
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

  [GeneratedRegex(
    @"/player/[^/]+/(?<id>[^/]+)/?",
    RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
  private static partial Regex PlayerPathRegex();
}
