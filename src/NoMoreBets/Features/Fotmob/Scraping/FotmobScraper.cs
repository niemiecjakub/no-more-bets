using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Features.Fotmob.Model;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Features.Fotmob.Scraping;

/// <summary>
/// FotMob scraper for fetching Premier League (or configured league) table and xG statistics.
/// </summary>
public class FotmobScraper : BaseScraper, IFotmobScraper
{
  private const string BaseUrl = "https://www.fotmob.com/en";

  private static readonly Regex TeamIdFromHrefRegex = new(@"/teams/(\d+)/", RegexOptions.Compiled);
  private static readonly Regex GoalsRegex = new(@"(\d+)\s*-\s*(\d+)", RegexOptions.Compiled);
  private static readonly Regex LeadingPositionRegex = new(@"^\s*(\d+)", RegexOptions.Compiled);
  private static readonly Regex OpponentFromMatchUrlRegex = new(@"/matches/([^/]+)-vs-([^/]+)/", RegexOptions.Compiled);
  private static readonly Regex TeamIdFromLogoUrlRegex = new(@"teamlogo/(\d+)", RegexOptions.Compiled);

  private static readonly IReadOnlyList<InteractionStep> FotmobConsentSteps =
  [
      new InteractionStep("button.fc-cta-consent", InteractionAction.Click, 600)
  ];

  private readonly FotmobScraperOptions _options;
  private readonly ILogger<FotmobScraper> _logger;

  public FotmobScraper(
      IHtmlCache cache,
      IPageFetcher fetcher,
      IInteractivePageFetcher interactiveFetcher,
      IOptions<BaseScraperOptions> baseOptions,
      IOptions<FotmobScraperOptions> fotmobOptions,
      ILogger<FotmobScraper> logger)
      : base(cache, fetcher, interactiveFetcher, baseOptions, logger)
  {
    _options = fotmobOptions.Value;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<Club>> GetLeagueTableAsync(TableFilter filter, CancellationToken cancellationToken = default)
  {
    var url = BuildTableUrl(filter);
    var html = await GetHtmlAfterInteractionsAsync(url, FotmobConsentSteps, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
    return await ParseLeagueTableClubsAsync(html).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<XgStats>> GetXgStatsAsync(CancellationToken cancellationToken = default)
  {
    var url = BuildXgUrl();
    var html = await GetHtmlAfterInteractionsAsync(url, FotmobConsentSteps, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
    return await ParseXgStatsAsync(html).ConfigureAwait(false);
  }

  internal async Task<IReadOnlyList<Club>> ParseLeagueTableClubsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var tableContainer = doc.QuerySelector("article.TableContainer");
    if (tableContainer is null)
      throw new InvalidOperationException("Table container not found in the page.");

    var rows = tableContainer.QuerySelectorAll("div[class*='TableRowCSS']");
    var clubs = new List<Club>();

    foreach (var row in rows)
    {
      try
      {
        var club = ParseLeagueTableRow(row);
        if (club is not null)
          clubs.Add(club);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to parse a league table row; skipping.");
      }
    }

    return clubs;
  }

  internal async Task<IReadOnlyList<XgStats>> ParseXgStatsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var tableContainer = doc.QuerySelector("article.TableContainer");
    if (tableContainer is null)
      throw new InvalidOperationException("Table container not found in the page.");

    var rows = tableContainer.QuerySelectorAll("div[class*='TableRowCSS'], tr[class*='TableRowCSS']");
    var list = new List<XgStats>();

    foreach (var row in rows)
    {
      try
      {
        var cells = row.QuerySelectorAll(":scope > div").ToArray();
        var statFromDivs = ParseXgRow(row, cells);

        if (statFromDivs is not null)
          list.Add(statFromDivs);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to parse an xG table row; skipping.");
      }
    }

    return list;
  }

  private string BuildTableUrl(TableFilter filter)
  {
    var path = $"{BaseUrl.TrimEnd('/')}/leagues/{_options.LeagueId}/table/{_options.LeagueSlug}";
    return filter switch
    {
      TableFilter.Home => path + "?filter=home",
      TableFilter.Away => path + "?filter=away",
      TableFilter.Form => path + "?filter=form",
      _ => path
    };
  }

  private string BuildXgUrl()
  {
    var path = $"{BaseUrl.TrimEnd('/')}/leagues/{_options.LeagueId}/table/{_options.LeagueSlug}";
    return path + "?filter=xg";
  }

  private Club? ParseLeagueTableRow(IElement row)
  {
    var positionCell = row.QuerySelector("div[class*='TablePositionCell']");
    if (positionCell is null)
      return null;
    if (!int.TryParse(positionCell.TextContent.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var position))
      return null;

    var teamCell = row.QuerySelector("div[class*='TableTeamCell']");
    var teamLink = teamCell?.QuerySelector("a[class*='TeamLink']");
    if (teamLink is null)
      return null;

    var href = teamLink.GetAttribute("href") ?? "";
    var teamId = 0;
    var teamIdMatch = TeamIdFromHrefRegex.Match(href);
    if (teamIdMatch.Success)
      int.TryParse(teamIdMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out teamId);

    var teamImg = teamLink.QuerySelector("img[class*='TeamIcon']");
    var teamLogoUrl = teamImg?.GetAttribute("src") ?? "";

    var teamNameElem = teamLink.QuerySelector("span[class*='TeamName']");
    var teamName = teamNameElem?.TextContent.Trim() ?? "";
    var teamShortnameElem = teamLink.QuerySelector("span[class*='TeamShortname']");
    var teamShortname = teamShortnameElem?.TextContent.Trim() ?? "";

    var allCells = row.Children.Where(c => c.TagName.Equals("DIV", StringComparison.OrdinalIgnoreCase)).ToArray();
    if (allCells.Length < 11)
      return null;

    var matchesPlayed = ExtractInt(allCells[2]);
    var wins = ExtractInt(allCells[3]);
    var draws = ExtractInt(allCells[4]);
    var losses = ExtractInt(allCells[5]);

    var goalsFor = 0;
    var goalsAgainst = 0;
    var goalsText = allCells[6].TextContent.Trim();
    var goalsMatch = GoalsRegex.Match(goalsText);
    if (goalsMatch.Success)
    {
      int.TryParse(goalsMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out goalsFor);
      int.TryParse(goalsMatch.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out goalsAgainst);
    }

    var goalDifference = allCells[7].TextContent.Trim();
    var points = ExtractInt(allCells[8]);

    var form = ParseForm(allCells[9]);
    ParseNextOpponent(allCells[10], out var nextOpponentId, out var nextOpponentName, out var nextOpponentLogoUrl);

    return new Club
    {
      Position = position,
      TeamName = teamName,
      TeamShortname = teamShortname,
      TeamId = teamId,
      TeamLogoUrl = teamLogoUrl,
      MatchesPlayed = matchesPlayed,
      Wins = wins,
      Draws = draws,
      Losses = losses,
      GoalsFor = goalsFor,
      GoalsAgainst = goalsAgainst,
      GoalDifference = goalDifference,
      Points = points,
      Form = form,
      NextOpponentId = nextOpponentId,
      NextOpponentName = nextOpponentName,
      NextOpponentLogoUrl = nextOpponentLogoUrl
    };
  }

  private static string ParseForm(IElement formCell)
  {
    var formSection = formCell.QuerySelector("section[class*='SingleTeamForm']");
    if (formSection is null)
      return "";

    var chars = new List<char>();
    foreach (var item in formSection.QuerySelectorAll("a[class*='ResultBox']"))
    {
      var cls = item.ClassName ?? "";
      if (cls.Contains("team-form__win", StringComparison.Ordinal))
        chars.Add('W');
      else if (cls.Contains("team-form__draw", StringComparison.Ordinal))
        chars.Add('D');
      else if (cls.Contains("team-form__loss", StringComparison.Ordinal))
        chars.Add('L');
    }
    return new string(chars.ToArray());
  }

  private static void ParseNextOpponent(IElement cell, out int? nextOpponentId, out string? nextOpponentName, out string? nextOpponentLogoUrl)
  {
    nextOpponentId = null;
    nextOpponentName = null;
    nextOpponentLogoUrl = null;

    var link = cell.QuerySelector("a[class*='NextOpponentCSS']");
    if (link is null)
      return;

    var oppImg = link.QuerySelector("img[class*='TeamIcon']");
    if (oppImg is not null)
    {
      nextOpponentLogoUrl = oppImg.GetAttribute("src");
      if (!string.IsNullOrEmpty(nextOpponentLogoUrl))
      {
        var logoMatch = TeamIdFromLogoUrlRegex.Match(nextOpponentLogoUrl);
        if (logoMatch.Success && int.TryParse(logoMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
          nextOpponentId = id;
      }
    }

    var oppHref = link.GetAttribute("href") ?? "";
    var matchUrlMatch = OpponentFromMatchUrlRegex.Match(oppHref);
    if (matchUrlMatch.Success)
    {
      var opponentSlug = matchUrlMatch.Groups[2].Value;
      nextOpponentName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(opponentSlug.Replace('-', ' '));
    }

    if (string.IsNullOrEmpty(nextOpponentName) && oppImg is not null)
    {
      var alt = oppImg.GetAttribute("alt")?.Trim();
      if (!string.IsNullOrEmpty(alt))
        nextOpponentName = alt;
    }
  }

  private static int ExtractInt(IElement element)
  {
    if (element is null)
      return 0;
    var text = element.TextContent.Trim();
    var digits = new string(text.Where(c => char.IsDigit(c) || c == '-').ToArray());
    return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
  }

  private XgStats? ParseXgRow(IElement row, IElement[] cells)
  {
    // Position is the first number before any '<' in the row's inner HTML.
    var innerHtml = row.InnerHtml ?? "";
    var idx = innerHtml.IndexOf('<');
    var beforeFirstTag = idx >= 0 ? innerHtml[..idx] : innerHtml;
    var positionMatch = LeadingPositionRegex.Match(beforeFirstTag.Trim());
    if (!positionMatch.Success ||
        !int.TryParse(positionMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var position))
      return null;

    int? positionChange = null;
    var chevronWrapper = (cells[0].ClassName ?? "").Contains("ChevronWrapper", StringComparison.Ordinal)
        ? cells[0]
        : cells[0].QuerySelector("div[class*='ChevronWrapper']");
    if (chevronWrapper is not null)
    {
      var span = chevronWrapper.QuerySelector("span");
      var changeText = span?.TextContent.Trim();
      if (!string.IsNullOrEmpty(changeText) && int.TryParse(changeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var change))
        positionChange = change;
    }

    var teamLink = cells[1].QuerySelector("a[class*='TeamLink']");
    if (teamLink is null)
      return null;

    return BuildXgStats(position, positionChange, teamLink, cells[2], cells[3], cells[4]);
  }

  private static XgStats? BuildXgStats(int position, int? positionChange, IElement teamLink, IElement xgCell, IElement xgaCell, IElement xptsCell)
  {
    var href = teamLink.GetAttribute("href") ?? "";
    var teamId = 0;
    var teamIdMatch = TeamIdFromHrefRegex.Match(href);
    if (teamIdMatch.Success)
      int.TryParse(teamIdMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out teamId);

    var teamImg = teamLink.QuerySelector("img[class*='TeamIcon']");
    var teamLogoUrl = teamImg?.GetAttribute("src") ?? "";
    var teamNameElem = teamLink.QuerySelector("span[class*='TeamName']");
    var teamName = teamNameElem?.TextContent.Trim() ?? "";
    var teamShortnameElem = teamLink.QuerySelector("span[class*='TeamShortname']");
    var teamShortname = teamShortnameElem?.TextContent.Trim() ?? "";

    if (!TryExtractXgValue(xgCell, out var xg, out var xgDiff))
      return null;
    if (!TryExtractXgValue(xgaCell, out var xga, out var xgaDiff))
      return null;
    if (!TryExtractXgValue(xptsCell, out var xpts, out var xptsDiff))
      return null;

    var positionChangeFormatted = positionChange is null
      ? null
      : positionChange.Value > 0
        ? "+" + positionChange.Value.ToString(CultureInfo.InvariantCulture)
        : positionChange.Value.ToString(CultureInfo.InvariantCulture);

    return new XgStats
    {
      Position = position,
      PositionChange = positionChangeFormatted,
      TeamId = teamId,
      TeamName = teamName,
      TeamShortname = teamShortname,
      TeamLogoUrl = teamLogoUrl,
      Xg = xg,
      XgDiff = xgDiff,
      Xga = xga,
      XgaDiff = xgaDiff,
      Xpts = xpts,
      XptsDiff = xptsDiff
    };
  }

  private static bool TryExtractXgValue(IElement cell, out double mainValue, out string? diffValue)
  {
    mainValue = 0;
    diffValue = null;

    var xgCellDiv = cell.QuerySelector("div[class*='XgCellCSS']")
        ?? (cell.TagName.Equals("DIV", StringComparison.OrdinalIgnoreCase) && (cell.ClassName ?? "").Contains("XgCellCSS", StringComparison.Ordinal)
            ? cell
            : null);
    var mainNumberSpan = xgCellDiv?.QuerySelector("span[class*='MainNumber']");
    if (mainNumberSpan is null)
      return false;

    var title = mainNumberSpan.GetAttribute("title");
    if (!string.IsNullOrEmpty(title) && double.TryParse(title, NumberStyles.Float, CultureInfo.InvariantCulture, out mainValue))
    { }
    else
    {
      var text = mainNumberSpan.TextContent.Trim();
      if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out mainValue))
        return false;
    }

    var diffSup = xgCellDiv?.QuerySelector("sup[class*='DiffText']");
    if (diffSup is not null)
    {
      var diffText = diffSup.TextContent.Trim();
      if (!string.IsNullOrEmpty(diffText))
        diffValue = diffText;
    }

    return true;
  }
}
