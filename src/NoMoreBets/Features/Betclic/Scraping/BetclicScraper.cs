using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Features.Betclic.Scraping;

/// <summary>
/// Betclic scraper for fetching Premier League upcoming games and match bookmaker events from betclic.pl.
/// </summary>
public class BetclicScraper : BaseScraper, IBetclicScraper
{
  private const string BaseUrl = "https://www.betclic.pl";
  private const string PremierLeagueUrl = BaseUrl + "/football-sfootball/premier-league-c3";

  private readonly IInteractivePageFetcher _interactiveFetcher;
  private readonly BetclicScraperOptions _betclicOptions;
  private readonly ILogger<BetclicScraper> _logger;

  private static readonly IReadOnlyList<InteractionStep> ExpandSteps =
  [
      new InteractionStep("#popin_tc_privacy_container_button button:nth-of-type(2)", InteractionAction.Click, 500),
        new InteractionStep("div.modal button", InteractionAction.Click, 500),
        new InteractionStep("button.is-seeMore, button[class*='seeMore'], button[class*='see-more']", InteractionAction.Click, 500)
  ];

  public BetclicScraper(
      IHtmlCache cache,
      IPageFetcher fetcher,
      IInteractivePageFetcher interactiveFetcher,
      IOptions<BaseScraperOptions> options,
      IOptions<BetclicScraperOptions> betclicOptions,
      ILogger<BetclicScraper> logger)
      : base(cache, fetcher, options, logger)
  {
    _interactiveFetcher = interactiveFetcher;
    _betclicOptions = betclicOptions.Value;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<UpcomingGame>> GetUpcomingGamesAsync(CancellationToken cancellationToken = default)
  {
    var games = new List<UpcomingGame>();
    for (var attempt = 0; attempt < _betclicOptions.EmptyResultRetryCount; attempt++)
    {
      var html = await GetPageHtmlAsync(PremierLeagueUrl, cancellationToken).ConfigureAwait(false);
      games = (await ParseUpcomingGamesAsync(html).ConfigureAwait(false)).ToList();
      if (games.Count > 0)
        return games;
      if (attempt < _betclicOptions.EmptyResultRetryCount - 1)
      {
        await ClearCacheAsync(PremierLeagueUrl, cancellationToken).ConfigureAwait(false);
        var delay = JitterDelay(_betclicOptions.EmptyResultRetryDelayMinSeconds, _betclicOptions.EmptyResultRetryDelayMaxSeconds);
        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
      }
    }
    return games;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<BookmakerEvent>> GetMatchEventsAsync(string gameUrl, bool expand, CancellationToken cancellationToken = default)
  {
    var events = new List<BookmakerEvent>();
    for (var attempt = 0; attempt < _betclicOptions.EmptyResultRetryCount; attempt++)
    {
      string html;
      if (expand)
      {
        var cached = await LoadFromCacheAsync(gameUrl, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
          html = cached;
        }
        else
        {
          var timeout = TimeSpan.FromSeconds(15);
          html = await _interactiveFetcher.GetHtmlAfterInteractionsAsync(gameUrl, ExpandSteps, timeout, cancellationToken).ConfigureAwait(false);
          await SaveToCacheAsync(gameUrl, html, cancellationToken).ConfigureAwait(false);
        }
      }
      else
      {
        html = await GetPageHtmlAsync(gameUrl, cancellationToken).ConfigureAwait(false);
      }

      var extracted = await ExtractEventsAsync(html).ConfigureAwait(false);
      events = AggregateEvents(extracted);
      if (events.Count > 0)
        return events;
      if (attempt < _betclicOptions.EmptyResultRetryCount - 1)
      {
        await ClearCacheAsync(gameUrl, cancellationToken).ConfigureAwait(false);
        var delay = JitterDelay(_betclicOptions.MatchEventsRetryDelayMinSeconds, _betclicOptions.MatchEventsRetryDelayMaxSeconds);
        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
      }
    }
    return events;
  }

  internal async Task<IReadOnlyList<UpcomingGame>> ParseUpcomingGamesAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
    var games = new List<UpcomingGame>();

    var groupEvents = doc.QuerySelector("div.groupEvents");
    if (groupEvents is null)
      return games;

    var dateHeader = groupEvents.QuerySelector("h2.groupEvents_headTitle");
    var date = dateHeader?.TextContent.Trim() ?? "";

    foreach (var card in groupEvents.QuerySelectorAll("sports-events-event-card.groupEvents_card"))
    {
      try
      {
        var link = card.QuerySelector("a.cardEvent");
        var href = link?.GetAttribute("href");
        var cardUrl = string.IsNullOrEmpty(href) ? "" : BaseUrl + href;

        var homeTeamElem = card.QuerySelector("[data-qa='contestant-1-label']");
        var awayTeamElem = card.QuerySelector("[data-qa='contestant-2-label']");
        var homeTeam = homeTeamElem?.TextContent.Trim() ?? "";
        var awayTeam = awayTeamElem?.TextContent.Trim() ?? "";

        var timeElem = card.QuerySelector("div.scoreboard_hour");
        var matchTime = timeElem?.TextContent.Trim() ?? "";

        double? homeOdds = null, drawOdds = null, awayOdds = null;
        var marketOdds = card.QuerySelector("div.market_odds");
        if (marketOdds is not null)
        {
          var oddsButtons = marketOdds.QuerySelectorAll("button.btn").Take(3).ToList();
          if (oddsButtons.Count >= 3)
          {
            homeOdds = ParseOddsFromButton(oddsButtons[0]);
            drawOdds = ParseOddsFromButton(oddsButtons[1]);
            awayOdds = ParseOddsFromButton(oddsButtons[2]);
          }
        }

        games.Add(new UpcomingGame
        {
          Date = date,
          HomeTeam = homeTeam,
          AwayTeam = awayTeam,
          Time = matchTime,
          HomeOdds = homeOdds,
          DrawOdds = drawOdds,
          AwayOdds = awayOdds,
          Url = cardUrl
        });
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to parse a game card; skipping.");
      }
    }
    return games;
  }

  private static double? ParseOddsFromButton(IElement button)
  {
    foreach (var span in button.QuerySelectorAll("span.btn_label, bcdk-bet-button-label.btn_label"))
    {
      if (span.ClassList.Contains("is-top"))
        continue;
      return ParseOdds(span.TextContent.Trim());
    }
    return null;
  }

  internal async Task<IReadOnlyList<BookmakerEvent>> ExtractEventsAsync(string html)
  {
    var context = BrowsingContext.New(Configuration.Default);
    var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
    var events = new List<BookmakerEvent>();

    foreach (var marketBox in doc.QuerySelectorAll("div.marketBox"))
    {
      var titleElem = marketBox.QuerySelector("h2.marketBox_headTitle");
      if (titleElem is null)
        continue;
      var title = titleElem.TextContent.Trim();

      var splitCards = marketBox.QuerySelectorAll("sports-split-card");
      if (splitCards.Length > 0)
      {
        foreach (var splitCard in splitCards)
        {
          var teamTitleElem = splitCard.QuerySelector("div.marketBox_bodyTitle");
          if (teamTitleElem is null)
            continue;
          var teamName = teamTitleElem.TextContent.Trim();
          var cardOptions = ParseMatrixOptions(splitCard);
          if (cardOptions.Count > 0)
            events.Add(new BookmakerEvent { Title = $"{title} - {teamName}", Options = cardOptions });
        }
        continue;
      }

      var isGrouped = marketBox.ClassList.Contains("is-groupedMarket") || marketBox.QuerySelector("div.marketBox_list") is not null;
      if (isGrouped)
      {
        var grouped = ParseGroupedMarketFromBox(marketBox, title);
        events.AddRange(grouped);
        continue;
      }

      IElement? container = null;
      var matrixMarkets = marketBox.QuerySelector("sports-matrix-markets");
      var spacedBlocks = marketBox.QuerySelector("sports-spaced-blocks");
      var simpleMarkets = marketBox.QuerySelector("sports-simple-markets");
      if (matrixMarkets is not null)
        container = matrixMarkets;
      else if (spacedBlocks is not null)
        container = spacedBlocks;
      else if (simpleMarkets is not null)
      {
        container = simpleMarkets.QuerySelector("div.marketBox_body") ?? simpleMarkets;
      }

      if (container is not null)
      {
        var options = ParseMatrixOptions(container);
        if (options.Count > 0)
          events.Add(new BookmakerEvent { Title = title, Options = options });
      }
      else
      {
        var marketBoxBody = marketBox.QuerySelector("div.marketBox_body");
        if (marketBoxBody is not null)
        {
          var options = ParseMatrixOptions(marketBoxBody);
          if (options.Count > 0)
            events.Add(new BookmakerEvent { Title = title, Options = options });
        }
      }
    }
    return events;
  }

  private static List<BookmakerEvent> ParseGroupedMarketFromBox(IElement marketBox, string title)
  {
    var events = new List<BookmakerEvent>();
    var subMarketItems = marketBox.QuerySelectorAll("span.marketBox_itemValue").Select(e => e.TextContent.Trim()).ToList();
    var lineSelections = marketBox.QuerySelectorAll("div.marketBox_lineSelection");

    foreach (var lineSelection in lineSelections)
    {
      var labelElem = lineSelection.QuerySelector("p.marketBox_label");
      if (labelElem is null)
        continue;
      var label = labelElem.TextContent.Trim();
      var marketItems = lineSelection.QuerySelectorAll("div.marketBox_item").ToArray();
      if (marketItems.Length != subMarketItems.Count)
        continue;
      for (var i = 0; i < marketItems.Length && i < subMarketItems.Count; i++)
      {
        var oddsElem = marketItems[i].QuerySelector("span.btn_label, bcdk-bet-button-label.btn_label");
        if (oddsElem is null)
          continue;
        var odds = ParseOdds(oddsElem.TextContent.Trim());
        if (odds is null)
          continue;
        events.Add(new BookmakerEvent { Title = title, Options = [new EventOption { Label = label, Odds = odds.Value }] });
      }
    }
    return events;
  }

  private static List<EventOption> ParseMatrixOptions(IElement container)
  {
    var options = new List<EventOption>();
    foreach (var lineSelection in container.QuerySelectorAll("div.marketBox_lineSelection"))
    {
      var labelElem = lineSelection.QuerySelector("p.marketBox_label");
      if (labelElem is null)
      {
        var button = lineSelection.QuerySelector("button.btn");
        if (button is not null)
        {
          foreach (var elem in lineSelection.QuerySelectorAll("span.btn_label, bcdk-bet-button-label.btn_label"))
          {
            if (elem.ClassList.Contains("is-top"))
            {
              labelElem = elem;
              break;
            }
          }
        }
      }
      if (labelElem is null)
        continue;
      var label = labelElem.TextContent.Trim();

      IElement? oddsElem = null;
      foreach (var elem in lineSelection.QuerySelectorAll("span.btn_label, bcdk-bet-button-label.btn_label"))
      {
        if (!elem.ClassList.Contains("is-top"))
        {
          oddsElem = elem;
          break;
        }
      }
      if (oddsElem is null)
        continue;
      var odds = ParseOdds(oddsElem.TextContent.Trim());
      if (odds is not null)
        options.Add(new EventOption { Label = label, Odds = odds.Value });
    }
    return options;
  }

  private static double? ParseOdds(string? oddsStr)
  {
    if (string.IsNullOrWhiteSpace(oddsStr))
      return null;
    var normalized = oddsStr.Replace(',', '.').Trim();
    return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
  }

  private static List<BookmakerEvent> AggregateEvents(IReadOnlyList<BookmakerEvent> events)
  {
    if (events.Count == 0)
      return [];
    var grouped = events.GroupBy(e => e.Title).ToList();
    var aggregated = new List<BookmakerEvent>();
    foreach (var g in grouped)
    {
      if (g.Count() == 1)
        aggregated.Add(g.First());
      else
        aggregated.Add(MergeEvents(g.ToList()));
    }
    return aggregated;
  }

  private static BookmakerEvent MergeEvents(IReadOnlyList<BookmakerEvent> events)
  {
    if (events.Count == 0)
      throw new ArgumentException("Cannot merge empty event list.", nameof(events));
    if (events.Count == 1)
      return events[0];
    var optionsDict = new Dictionary<(string Label, double Odds), EventOption>();
    foreach (var ev in events)
    {
      foreach (var opt in ev.Options)
      {
        var key = (opt.Label, opt.Odds);
        if (!optionsDict.ContainsKey(key))
          optionsDict[key] = opt;
      }
    }
    var sorted = optionsDict.Values.OrderBy(x => x.Label).ToList();
    return new BookmakerEvent { Title = events[0].Title, Options = sorted };
  }

  private static double JitterDelay(double minSeconds, double maxSeconds)
  {
    return minSeconds + (Random.Shared.NextDouble() * (maxSeconds - minSeconds));
  }
}
