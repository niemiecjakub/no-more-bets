using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Clubs;
using NoMoreBets.Application.Fotmob;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

/// <summary>
/// FotMob scraper for fetching Premier League table and xG statistics.
/// </summary>
public class FotmobScraper : BaseScraper, ILeagueProvider, IClubOverviewProvider, IMatchDetailsProvider
{
  private const string BaseUrl = "https://www.fotmob.com";

  private static readonly Regex TeamIdFromHrefRegex = new(@"/teams/(\d+)/", RegexOptions.Compiled);
  private static readonly Regex GoalsRegex = new(@"(\d+)\s*-\s*(\d+)", RegexOptions.Compiled);
  private static readonly Regex LeadingPositionRegex = new(@"^\s*(\d+)", RegexOptions.Compiled);
  private static readonly Regex OpponentFromMatchUrlRegex = new(@"/matches/([^/]+)-vs-([^/]+)/", RegexOptions.Compiled);
  private static readonly Regex TeamIdFromLogoUrlRegex = new(@"teamlogo/(\d+)", RegexOptions.Compiled);

  private static readonly IReadOnlyList<InteractionStep> FotmobConsentSteps =
  [
      new InteractionStep("button.fc-cta-consent", InteractionAction.Click, 600)
  ];

  private readonly ILogger<FotmobScraper> _logger;
  private readonly IFotmobConstants _fotmobConstants;

  public FotmobScraper(
      PlaywrightPageFetcher pageFetcher,
      IOptions<BaseScraperOptions> baseOptions,
      IFotmobConstants fotmobConstants,
      ILogger<FotmobScraper> logger)
      : base(pageFetcher, baseOptions, logger)
  {
    _logger = logger;
    _fotmobConstants = fotmobConstants;
  }

  /// <summary>Gets the league table (standings) for the configured league, optionally filtered by home/away/form.</summary>
  public async Task<IReadOnlyList<TableEntry>> GetLeagueTableAsync(CancellationToken cancellationToken = default)
  {
    var url = $"{BaseUrl}/leagues/{_fotmobConstants.PremierLeague.Id}/table/{_fotmobConstants.PremierLeague.Slug}";
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

  /// <inheritdoc />
  public async Task<ClubOverview> GetClubOverviewAsync(int teamId, CancellationToken cancellationToken = default)
  {
    var url = BuildTeamOverviewUrl(teamId);
    var html = await GetHtmlAfterInteractionsAsync(url, FotmobConsentSteps, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
    return await ParseClubOverviewAsync(html).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task<MatchDetailsDto> GetMatchDetailsAsync(string gameUrl, CancellationToken cancellationToken = default)
  {
    var html = await GetHtmlAfterInteractionsAsync(gameUrl, FotmobConsentSteps, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
    var details = await ParseMatchDetailsAsync(html).ConfigureAwait(false);

    IReadOnlyList<StatGroup>? statistics = null;
    IReadOnlyList<PlayerMatchStats>? players = null;
    try
    {
      var statsUrl = gameUrl + ":tab=stats";
      var statsHtml = await GetHtmlAfterInteractionsAsync(statsUrl, FotmobConsentSteps, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
      statistics = await ParseStatisticsFromDocumentAsync(statsHtml).ConfigureAwait(false);
      players = await ParsePlayersFromDocumentAsync(statsHtml).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to fetch or parse match statistics; returning match details without statistics.");
    }

    return new MatchDetailsDto
    {
      HomeTeam = details.HomeTeam,
      AwayTeam = details.AwayTeam,
      MatchDate = details.MatchDate,
      HomeScore = details.HomeScore,
      AwayScore = details.AwayScore,
      HomeLineup = details.HomeLineup,
      AwayLineup = details.AwayLineup,
      Statistics = statistics,
      Players = players
    };
  }

  internal async Task<MatchDetailsDto> ParseMatchDetailsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    ParseGeneralInfo(doc, out var homeTeam, out var awayTeam, out var matchDate);
    ParseLineups(doc, out var homeLineup, out var awayLineup);

    int? homeScore = null;
    int? awayScore = null;
    var scoreEl = doc.QuerySelector("[class*='MFHeaderStatusScore']");
    var scoreText = scoreEl?.TextContent.Trim();
    if (!string.IsNullOrEmpty(scoreText))
    {
      var scoreMatch = GoalsRegex.Match(scoreText);
      if (scoreMatch.Success &&
          int.TryParse(scoreMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var home) &&
          int.TryParse(scoreMatch.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var away))
      {
        homeScore = home;
        awayScore = away;
      }
    }

    return new MatchDetailsDto
    {
      HomeTeam = homeTeam,
      AwayTeam = awayTeam,
      MatchDate = matchDate,
      HomeScore = homeScore,
      AwayScore = awayScore,
      HomeLineup = homeLineup,
      AwayLineup = awayLineup,
    };
  }

  /// <summary>Parses all StatGroupContainer elements from Statistics tab HTML. Returns empty list if none found.</summary>
  internal static async Task<IReadOnlyList<StatGroup>> ParseStatisticsFromDocumentAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
    var containers = doc.QuerySelectorAll("[class*='StatGroupContainer']");
    var groups = new List<StatGroup>();

    foreach (var container in containers)
    {
      var containerGroups = ParseStatGroupsFromContainer(container);
      groups.AddRange(containerGroups);
    }

    return groups;
  }

  /// <summary>Parses the player stats table from Statistics tab HTML. Returns empty list if table not found.</summary>
  internal static async Task<IReadOnlyList<PlayerMatchStats>> ParsePlayersFromDocumentAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
    var table = doc.QuerySelector("table[class*='StyledTable']") ?? doc.QuerySelector("[class*='StyledTable']");
    var list = new List<PlayerMatchStats>();
    if (table is null)
      return list;
    var rows = table.QuerySelectorAll("tbody tr");

    foreach (var row in rows)
    {
      var cells = row.QuerySelectorAll("td").ToArray();
      if (cells.Length < 9)
        continue;

      var player = cells[0].QuerySelector("[class*='PlayerNameCSS']")?.TextContent.Trim() ?? "";
      var score = cells[1].QuerySelector("[class*='PlayerRatingCSS'] span")?.TextContent.Trim() ?? GetCellText(cells[1]);
      var minutesPlayed = GetCellText(cells[2]);
      var goals = GetCellText(cells[3]);
      var assists = GetCellText(cells[4]);
      var xg = GetCellText(cells[5]);
      var xa = GetCellText(cells[6]);
      var xgPlusXa = GetCellText(cells[7]);
      var defensiveContributions = GetCellText(cells[8]);

      list.Add(new PlayerMatchStats
      {
        Player = player,
        Score = score,
        MinutesPlayed = minutesPlayed,
        Goals = goals,
        Assists = assists,
        Xg = xg,
        Xa = xa,
        XgPlusXa = xgPlusXa,
        DefensiveContributions = defensiveContributions
      });
    }

    return list;
  }

  private static string GetCellText(IElement cell)
  {
    var span = cell.QuerySelector("span");
    return span?.TextContent.Trim() ?? "";
  }

  /// <summary>Parses one ul (StatGroupContainer) into zero or more StatGroups (section-aware).</summary>
  private static List<StatGroup> ParseStatGroupsFromContainer(IElement ul)
  {
    var result = new List<StatGroup>();
    var currentTitle = "";
    var currentRows = new List<StatRow>();
    var children = ul.Children.ToArray();

    void PushCurrentGroup()
    {
      if (currentRows.Count > 0)
      {
        result.Add(new StatGroup { Title = currentTitle, Rows = currentRows.ToList() });
        currentRows = new List<StatRow>();
      }
    }

    for (var i = 0; i < children.Length; i++)
    {
      var child = children[i];
      var tag = child.TagName?.ToUpperInvariant() ?? "";
      var className = child.ClassName ?? "";

      if (tag == "HEADER")
      {
        var titleEl = child.QuerySelector("h2") ?? child.QuerySelector("[class*='Title']");
        currentTitle = titleEl?.TextContent.Trim() ?? "";
        continue;
      }

      if (className.Contains("PossessionTitle", StringComparison.OrdinalIgnoreCase))
      {
        var labelEl = child.QuerySelector("[class*='StatTitle']");
        var label = labelEl?.TextContent.Trim() ?? "";
        IElement? possessionDiv = null;
        if (i + 1 < children.Length && (children[i + 1].ClassName ?? "").Contains("PossessionDiv", StringComparison.OrdinalIgnoreCase))
          possessionDiv = children[i + 1];
        if (possessionDiv is not null)
        {
          var segments = possessionDiv.QuerySelectorAll("[class*='PossessionSegment'] span").ToArray();
          var homeVal = segments.Length > 0 ? segments[0].TextContent.Trim() : null;
          var awayVal = segments.Length > 1 ? segments[1].TextContent.Trim() : null;
          currentRows.Add(new StatRow { Label = label, HomeValue = homeVal, AwayValue = awayVal });
          i++;
        }
        continue;
      }

      if (tag == "DIV" && (className.Contains("Separator", StringComparison.OrdinalIgnoreCase) ||
                           className.Contains("XgWrapper", StringComparison.OrdinalIgnoreCase) ||
                           className.Contains("InfoWrapper", StringComparison.OrdinalIgnoreCase)))
        continue;

      if (tag == "LI" && className.Contains("Stat", StringComparison.OrdinalIgnoreCase))
      {
        var labelEl = child.QuerySelector("[class*='StatTitle']");
        var label = labelEl?.TextContent.Trim() ?? "";
        var boxes = child.QuerySelectorAll("[class*='StatBox']").ToArray();
        var firstValue = boxes.Length > 0 ? boxes[0].QuerySelector("[class*='StatValue']")?.TextContent.Trim() : null;
        var secondValue = boxes.Length > 1 ? boxes[1].QuerySelector("[class*='StatValue']")?.TextContent.Trim() : null;
        var isHeaderRow = (string.IsNullOrEmpty(firstValue) && string.IsNullOrEmpty(secondValue)) ||
                          (labelEl?.ClassName ?? "").Contains("dnc4li", StringComparison.OrdinalIgnoreCase);

        if (isHeaderRow)
        {
          PushCurrentGroup();
          currentTitle = label;
        }
        else if (!string.IsNullOrEmpty(label))
        {
          currentRows.Add(new StatRow { Label = label, HomeValue = firstValue, AwayValue = secondValue });
        }
        continue;
      }
    }

    PushCurrentGroup();
    return result;
  }

  private static void ParseGeneralInfo(IDocument doc, out string homeTeam, out string awayTeam, out DateTimeOffset? matchDate)
  {
    homeTeam = "";
    awayTeam = "";
    matchDate = null;

    var timeEl = doc.QuerySelector("time[datetime]");
    if (timeEl is not null)
    {
      var datetime = timeEl.GetAttribute("datetime");
      if (!string.IsNullOrEmpty(datetime) && DateTimeOffset.TryParse(datetime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        matchDate = parsed;
    }

    var h1 = doc.QuerySelector("h1");
    var h1Text = h1?.TextContent?.Trim() ?? "";
    if (string.IsNullOrEmpty(h1Text))
      return;

    var vsIndex = h1Text.IndexOf(" vs ", StringComparison.OrdinalIgnoreCase);
    if (vsIndex < 0)
      return;
    homeTeam = h1Text[..vsIndex].Trim();
    var afterVs = h1Text[(vsIndex + 4)..].Trim();
    var parenIndex = afterVs.IndexOf(" (", StringComparison.Ordinal);
    awayTeam = parenIndex >= 0 ? afterVs[..parenIndex].Trim() : afterVs;

    if (matchDate is null && parenIndex >= 0)
    {
      var datePart = afterVs[(parenIndex + 2)..].TrimEnd(')');
      if (!string.IsNullOrEmpty(datePart) && DateTimeOffset.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromH1))
        matchDate = fromH1;
    }
  }

  private void ParseLineups(IDocument doc, out TeamLineup? homeLineup, out TeamLineup? awayLineup)
  {
    homeLineup = null;
    awayLineup = null;

    var lineupSection = doc.QuerySelector("[class*='LineupCSS']") ?? doc.QuerySelector("[class*='LineupBackground']");
    if (lineupSection is null)
      return;

    var teamInfoContainers = lineupSection.QuerySelectorAll("[class*='TeamInfoContainer']").ToArray();
    var teamContainers = lineupSection.QuerySelectorAll("[class*='TeamContainer']").ToArray();

    if (teamInfoContainers.Length >= 2 && teamContainers.Length >= 2)
    {
      homeLineup = ParseTeamLineup(teamInfoContainers[0], teamContainers[0]);
      awayLineup = ParseTeamLineup(teamInfoContainers[1], teamContainers[1]);
    }
  }

  private static TeamLineup? ParseTeamLineup(IElement teamInfoContainer, IElement teamContainer)
  {
    var teamName = "";
    var link = teamInfoContainer.QuerySelector("a[class*='LineupContainer']");
    var h2 = link?.QuerySelector("h2");
    if (h2 is not null)
      teamName = h2.TextContent.Trim();

    var formationEl = teamInfoContainer.QuerySelector("[class*='FormationText']");
    var formation = formationEl?.TextContent.Trim();

    double? teamRating = null;
    var badgeWrapper = teamInfoContainer.QuerySelector("[class*='BadgeWrapper']");
    var ratingSpan = badgeWrapper?.QuerySelector("[class*='PlayerRatingCSS'] span");
    if (ratingSpan is not null && TryParseRating(ratingSpan.TextContent, out var tr))
      teamRating = tr;

    var players = new List<LineupPlayer>();
    foreach (var playerDiv in teamContainer.QuerySelectorAll("[class*='PlayerDiv']"))
    {
      var player = ParseLineupPlayer(playerDiv);
      if (player is not null)
        players.Add(player);
    }

    return new TeamLineup
    {
      TeamName = teamName,
      Formation = formation,
      TeamRating = teamRating,
      Players = players
    };
  }

  private static LineupPlayer? ParseLineupPlayer(IElement playerDiv)
  {
    var nameEl = playerDiv.QuerySelector("[class*='LineupPlayerText']");
    var name = nameEl?.GetAttribute("title")?.Trim() ?? nameEl?.TextContent.Trim() ?? "";

    double? rating = null;
    var ratingEl = playerDiv.QuerySelector("[class*='PlayerRatingCSS'] span");
    if (ratingEl is not null && TryParseRating(ratingEl.TextContent, out var r))
      rating = r;

    return new LineupPlayer
    {
      Name = name,
      Rating = rating
    };
  }

  private static bool TryParseRating(string? text, out double value)
  {
    value = 0;
    if (string.IsNullOrWhiteSpace(text))
      return false;
    var normalized = text.Trim().Replace(',', '.');
    return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
  }

  internal async Task<ClubOverview> ParseClubOverviewAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var recentGames = ParseRecentGamesFromDocument(doc);
    var dailySummary = ParseDailySummaryFromDocument(doc);

    return new ClubOverview
    {
      RecentGames = recentGames,
      DailySummary = dailySummary
    };
  }

  private IReadOnlyList<ClubRecentGame> ParseRecentGamesFromDocument(IDocument doc)
  {
    var links = doc.QuerySelectorAll("a[class*='TeamFormMatchLink']").ToArray();
    var takeCount = Math.Min(5, links.Length);
    var startIndex = links.Length - takeCount;
    var results = new List<ClubRecentGame>();

    for (var i = startIndex; i < links.Length; i++)
    {
      try
      {
        var game = ParseClubRecentGameLink(links[i]);
        if (game is not null)
          results.Add(game);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to parse a club recent game link; skipping.");
      }
    }

    return results;
  }

  private static IReadOnlyList<string> ParseDailySummaryFromDocument(IDocument doc)
  {
    var list = new List<string>();
    var container = doc.QuerySelector("div[class*='NewsSummaryContainerCSS']");
    var liElements = container?.QuerySelectorAll("ul[class*='NewsList'] li") ?? doc.QuerySelectorAll("ul[class*='NewsList'] li");

    foreach (var li in liElements)
    {
      var text = li.TextContent.Trim();
      if (string.IsNullOrEmpty(text))
        continue;
      if (text.EndsWith("Więcej", StringComparison.OrdinalIgnoreCase))
        text = text[..^6].TrimEnd('\u00A0', ' ');
      list.Add(text);
    }

    return list;
  }

  internal async Task<IReadOnlyList<TableEntry>> ParseLeagueTableClubsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);

    var tableContainer = doc.QuerySelector("article.TableContainer");
    if (tableContainer is null)
      throw new InvalidOperationException("Table container not found in the page.");

    var rows = tableContainer.QuerySelectorAll("div[class*='TableRowCSS']");
    var clubs = new List<TableEntry>();

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

  private static ClubRecentGame? ParseClubRecentGameLink(IElement link)
  {
    var href = link.GetAttribute("href") ?? "";
    if (string.IsNullOrEmpty(href))
      return null;

    var gameUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
      ? href
      : BaseUrl + (href.StartsWith("/", StringComparison.Ordinal) ? href : "/" + href);

    var scoreSpan = link.QuerySelector("span[class*='ScoreSpan']");
    var score = scoreSpan?.TextContent.Trim() ?? "";

    var result = MatchResult.Draw;
    var statusWrapper = link.QuerySelector("div[class*='FixtureStatusWrapper']");
    var colorDiv = statusWrapper?.QuerySelector("div[color]");
    var color = colorDiv?.GetAttribute("color") ?? "";
    if (color.Contains("TeamForm-green", StringComparison.Ordinal))
      result = MatchResult.Win;
    else if (color.Contains("TeamForm-red", StringComparison.Ordinal))
      result = MatchResult.Loss;
    else if (color.Contains("TeamForm-grey", StringComparison.Ordinal))
      result = MatchResult.Draw;

    var img = link.QuerySelector("img[class*='TeamIcon']") ?? link.QuerySelector("img");
    var src = img?.GetAttribute("src") ?? "";
    if (string.IsNullOrEmpty(src))
      return null;
    var logoMatch = TeamIdFromLogoUrlRegex.Match(src);
    if (!logoMatch.Success || !int.TryParse(logoMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var opponentId))
      return null;

    return new ClubRecentGame
    {
      OpponentId = opponentId,
      Score = score,
      Result = result,
      GameUrl = gameUrl
    };
  }

  private string BuildXgUrl()
  {
    var path = $"{BaseUrl}/leagues/{_fotmobConstants.PremierLeague.Id}/table/{_fotmobConstants.PremierLeague.Slug}";
    return path + "?filter=xg";
  }

  private static string BuildTeamOverviewUrl(int teamId)
  {
    return $"{BaseUrl}/teams/{teamId}";
  }

  private TableEntry? ParseLeagueTableRow(IElement row)
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

    return new TableEntry
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

  private static IReadOnlyList<MatchResult> ParseForm(IElement formCell)
  {
    var formSection = formCell.QuerySelector("section[class*='SingleTeamForm']");
    if (formSection is null)
      return Array.Empty<MatchResult>();

    var results = new List<MatchResult>();
    foreach (var item in formSection.QuerySelectorAll("a[class*='ResultBox']"))
    {
      var cls = item.ClassName ?? "";
      if (cls.Contains("team-form__win", StringComparison.Ordinal))
        results.Add(MatchResult.Win);
      else if (cls.Contains("team-form__draw", StringComparison.Ordinal))
        results.Add(MatchResult.Draw);
      else if (cls.Contains("team-form__loss", StringComparison.Ordinal))
        results.Add(MatchResult.Loss);
    }
    return results;
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
    // CurrentPostition is the first number before any '<' in the row's inner HTML.
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
