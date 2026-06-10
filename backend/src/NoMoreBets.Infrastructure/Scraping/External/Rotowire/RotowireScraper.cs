using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NoMoreBets.Infrastructure.Scraping.External.Rotowire;

/// <summary>
/// RotoWire scraper for fetching and parsing soccer lineup data from rotowire.com (one lineup page per configured league).
/// </summary>
public class RotowireScraper : BaseScraper, ILineupProvider
{
  private const string BaseUrl = "https://www.rotowire.com";

  /// <summary>
  /// Per-league lineup page URLs (slugs align with <c>BetclicScraper</c> league keys).
  /// </summary>
  private static readonly IReadOnlyDictionary<string, string> LeagueLineupUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
  {
    ["premier-league"] = BaseUrl + "/soccer/lineups.php",
    ["laliga"] = BaseUrl + "/soccer/lineups.php?league=LIGA",
    ["bundesliga"] = BaseUrl + "/soccer/lineups.php?league=BUND",
    ["serie-a"] = BaseUrl + "/soccer/lineups.php?league=SERI",
    ["ligue-1"] = BaseUrl + "/soccer/lineups.php?league=FRAN",
    ["fifa-world-cup"] = BaseUrl + "/soccer/lineups.php?league=WOC"
  };

  private static readonly Regex DateRegex = new(@"(\w+\s+\d+)", RegexOptions.Compiled);
  private static readonly Regex TimeRegex = new(@"(\d+:\d+\s+(?:AM|PM)\s+ET)", RegexOptions.Compiled);

  private readonly ILogger<RotowireScraper> _logger;

  /// <inheritdoc />
  public IReadOnlyCollection<string> SupportedLeagueSlugs => LeagueLineupUrls.Keys.ToArray();

  public RotowireScraper(
      PlaywrightPageFetcher pageFetcher,
      IOptions<BaseScraperOptions> options,
      ILogger<RotowireScraper> logger)
      : base(pageFetcher, options, logger)
  {
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync(string leagueSlug, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(leagueSlug))
    {
      throw new ArgumentException("League slug is required.", nameof(leagueSlug));
    }

    if (!LeagueLineupUrls.TryGetValue(leagueSlug, out var url))
    {
      throw new NotSupportedException(
        $"RotoWire lineups are not supported for league slug '{leagueSlug}'. Supported slugs: {string.Join(", ", LeagueLineupUrls.Keys)}.");
    }

    if (string.IsNullOrWhiteSpace(url))
    {
      _logger.LogWarning("Rotowire lineup URL empty for league slug {LeagueSlug}; returning no games.", leagueSlug);
      return [];
    }

    var html = await GetPageHtmlAsync(url, cancellationToken).ConfigureAwait(false);
    var games = (await ParseLineupsAsync(html).ConfigureAwait(false)).ToList();
    if (games.Count == 0)
    {
      _logger.LogWarning(
        "Rotowire lineup scrape completed with zero games for league {LeagueSlug}. Url: {Url}",
        leagueSlug,
        url);
    }

    return games;
  }

  internal async Task<IReadOnlyList<GameLineup>> ParseLineupsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
    var games = new List<GameLineup>();

    foreach (var lineupDiv in doc.QuerySelectorAll("div.lineup.is-soccer"))
    {
      try
      {
        var game = ParseGameSection(lineupDiv);
        if (game is not null)
          games.Add(game);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to parse a game section; skipping.");
      }
    }

    return games;
  }

  private GameLineup? ParseGameSection(IElement section)
  {
    var date = (string?)null;
    var time = (string?)null;
    var timeElem = section.QuerySelector("div.lineup__time");
    if (timeElem is not null)
    {
      var timeText = timeElem.TextContent.Trim();
      var dateMatch = DateRegex.Match(timeText);
      if (dateMatch.Success)
        date = dateMatch.Groups[1].Value;
      var timeMatch = TimeRegex.Match(timeText);
      if (timeMatch.Success)
        time = timeMatch.Groups[1].Value;
    }

    var teamAbbrs = section.QuerySelectorAll("div.lineup__abbr").ToList();
    string? homeCode = null;
    string? awayCode = null;
    if (teamAbbrs.Count >= 2)
    {
      homeCode = teamAbbrs[0].TextContent.Trim();
      awayCode = teamAbbrs[1].TextContent.Trim();
    }

    var homeTeamElem = section.QuerySelector("div.lineup__mteam.is-home");
    var awayTeamElem = section.QuerySelector("div.lineup__mteam.is-visit");
    var homeTeamName = homeTeamElem?.TextContent.Trim();
    var awayTeamName = awayTeamElem?.TextContent.Trim();

    if (string.IsNullOrEmpty(homeCode) || string.IsNullOrEmpty(awayCode))
    {
      var sectionTime = section.QuerySelector("div.lineup__time")?.TextContent.Trim();
      _logger.LogWarning(
        "Skipping Rotowire game section due to missing team codes. TimeLabel: {TimeLabel}",
        string.IsNullOrWhiteSpace(sectionTime) ? "<unknown>" : sectionTime);
      return null;
    }

    var homeLineup = ParseTeamLineup(section, homeCode);
    var awayLineup = ParseTeamLineup(section, awayCode);

    var homeName = homeTeamName ?? $"Team {homeCode}";
    var awayName = awayTeamName ?? $"Team {awayCode}";
    ValiiadteLineupPlayers(homeLineup, awayLineup, homeName, awayName);

    var parsedDate = ParseRotowireDate(date);
    return new GameLineup
    {
      Date = parsedDate,
      Time = time,
      HomeTeamName = homeName,
      HomeTeamCode = homeCode,
      HomeTeam = homeLineup,
      AwayTeamName = awayName,
      AwayTeamCode = awayCode,
      AwayTeam = awayLineup
    };
  }

  private static DateTime ParseRotowireDate(string? dateStr)
  {
    if (string.IsNullOrWhiteSpace(dateStr))
    {
      return DateTime.Today;
    }
    var withYear = $"{dateStr.Trim()}, {DateTime.Today.Year}";
    if (DateTime.TryParse(withYear, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var dt))
    {
      return dt;
    }

    throw new ArgumentException($"Couldnt parse date from rotowire: {dateStr}", nameof(dateStr));
  }

  private TeamLineup ParseTeamLineup(IElement section, string teamCode)
  {
    var players = new List<PlayerInLineup>();
    var injuries = new List<InjuryEntry>();
    var lineupType = LineupType.Unknown;

    var homeTeamElem = section.QuerySelector("div.lineup__team.is-home");
    var isHome = false;
    if (homeTeamElem is not null)
    {
      var homeAbbr = homeTeamElem.QuerySelector("div.lineup__abbr");
      if (homeAbbr is not null && homeAbbr.TextContent.Trim().Equals(teamCode, StringComparison.Ordinal))
        isHome = true;
    }

    var listClass = isHome ? "lineup__list is-home" : "lineup__list is-visit";
    var lineupList = section.QuerySelector($"ul.{listClass.Replace(" ", ".")}");
    if (lineupList is null)
    {
      foreach (var ul in section.QuerySelectorAll("ul.lineup__list"))
      {
        if (ul.TextContent.Contains(teamCode, StringComparison.Ordinal))
        {
          lineupList = ul;
          break;
        }
      }
    }

    if (lineupList is null)
    {
      _logger.LogWarning(
        "Rotowire lineup list not found for team code {TeamCode}; returning empty lineup.",
        teamCode);
      return new TeamLineup
      {
        LineupType = lineupType,
        Players = players,
        Injuries = injuries
      };
    }

    var statusElem = lineupList.QuerySelector("li.lineup__status");
    if (statusElem is not null)
    {
      var statusText = statusElem.TextContent.Trim();
      if (LineupTypes.TryParseFromStatusText(statusText, out var parsed))
        lineupType = parsed;
    }

    var inInjuriesSection = false;
    foreach (var li in lineupList.QuerySelectorAll("li"))
    {
      var hasTitle = li.ClassList.Contains("lineup__title");
      if (hasTitle && li.TextContent.Contains("Injuries", StringComparison.Ordinal))
      {
        inInjuriesSection = true;
        continue;
      }

      if (li.ClassList.Contains("lineup__status"))
        continue;

      if (!li.ClassList.Contains("lineup__player"))
        continue;

      var posElem = li.QuerySelector("div.lineup__pos");
      var nameElem = li.QuerySelector("a");
      var injuryElem = li.QuerySelector("span.lineup__inj");

      if (posElem is null || nameElem is null)
        continue;

      var acronym = posElem.TextContent.Trim();
      var playerName = nameElem.TextContent.Trim();
      FootballPositions.TryParseFromAcronym(acronym, out var position);
      if (position == FootballPosition.Unknown)
        _logger.LogError("Unknown position acronym \"{Acronym}\" for player {Name}", acronym, playerName);

      if (inInjuriesSection)
      {
        if (injuryElem is not null)
        {
          var statusText = injuryElem.TextContent.Trim();
          if (!InjuryStatuses.TryParseFromCode(statusText, out var injuryStatus))
          {
            _logger.LogWarning("Unknown injury status \"{Status}\" for player {Name}", statusText, playerName);
            injuryStatus = InjuryStatus.Unknown;
          }
          injuries.Add(new InjuryEntry(position, playerName, injuryStatus));
        }
      }
      else
      {
        players.Add(new PlayerInLineup(position, playerName));
      }
    }

    return new TeamLineup
    {
      LineupType = lineupType,
      Players = players,
      Injuries = injuries
    };
  }

  private void ValiiadteLineupPlayers(TeamLineup homeLineup, TeamLineup awayLineup, string homeTeamName, string awayTeamName)
  {
    if (homeLineup.Players.Count != 11)
      _logger.LogError("{Team} lineup has {Count} players (expected 11)", homeTeamName, homeLineup.Players.Count);
    if (awayLineup.Players.Count != 11)
      _logger.LogError("{Team} lineup has {Count} players (expected 11)", awayTeamName, awayLineup.Players.Count);
  }
}
