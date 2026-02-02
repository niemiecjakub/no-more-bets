using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Features.Rotowire.Scraping;

/// <summary>
/// RotoWire scraper for fetching and parsing soccer lineup data from rotowire.com.
/// </summary>
public class RotowireScraper : BaseScraper, IRotowireScraper
{
  private const string BaseUrl = "https://www.rotowire.com";
  private const string LineupsUrl = BaseUrl + "/soccer/lineups.php";

  private static readonly Regex DateRegex = new(@"(\w+\s+\d+)", RegexOptions.Compiled);
  private static readonly Regex TimeRegex = new(@"(\d+:\d+\s+(?:AM|PM)\s+ET)", RegexOptions.Compiled);

  private readonly ILogger<RotowireScraper> _logger;

  public RotowireScraper(
      IHtmlCache cache,
      IPageFetcher fetcher,
      IInteractivePageFetcher interactiveFetcher,
      IOptions<BaseScraperOptions> options,
      ILogger<RotowireScraper> logger)
      : base(cache, fetcher, interactiveFetcher, options, logger)
  {
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync(CancellationToken cancellationToken = default)
  {
    var html = await GetPageHtmlAsync(LineupsUrl, cancellationToken).ConfigureAwait(false);
    return await ParseLineupsAsync(html).ConfigureAwait(false);
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
      return null;

    var homeLineup = ParseTeamLineup(section, homeCode, homeTeamName ?? $"Team {homeCode}");
    var awayLineup = ParseTeamLineup(section, awayCode, awayTeamName ?? $"Team {awayCode}");

    ValiiadteLineupPlayers(homeLineup, awayLineup);

    return new GameLineup
    {
      Date = string.IsNullOrEmpty(date) ? "Unknown" : date,
      Time = time,
      HomeTeam = homeLineup,
      AwayTeam = awayLineup
    };
  }

  private TeamLineup ParseTeamLineup(IElement section, string teamCode, string teamName)
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
      return new TeamLineup
      {
        TeamName = teamName,
        TeamCode = teamCode,
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
        _logger.LogError("Unknown position acronym \"{Acronym}\" for player {Player}", acronym, playerName);

      if (inInjuriesSection)
      {
        if (injuryElem is not null)
        {
          var statusText = injuryElem.TextContent.Trim();
          if (!InjuryStatuses.TryParseFromCode(statusText, out var injuryStatus))
          {
            _logger.LogWarning("Unknown injury status \"{Status}\" for player {Player}", statusText, playerName);
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
      TeamName = teamName,
      TeamCode = teamCode,
      LineupType = lineupType,
      Players = players,
      Injuries = injuries
    };
  }

  private void ValiiadteLineupPlayers(params TeamLineup[] lineups)
  {
    foreach (var lineup in lineups)
    {
      if (lineup.Players.Count != 11)
      {
        _logger.LogError("{Team} lineup has {Count} players (expected 11)", lineup.TeamName, lineup.Players.Count);
      }
    }
  }
}
