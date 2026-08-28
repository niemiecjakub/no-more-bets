namespace NoMoreBets.Application.AgentTools;

public static class AgentToolCatalog
{
  public static class Match
  {
    public static readonly AgentToolDefinition GetLineups =
      new("match_getLineups", "Look up lineups", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetInjuries =
      new("match_getInjuries", "Check injuries", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetHead2HeadStats =
      new("match_getHead2HeadStats", "Review head-to-head", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetClubDailySummary =
      new("match_getClubDailySummary", "Read club daily summary", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetClubRecentGames =
      new("match_getClubRecentGames", "Review recent form", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetClubLeagueStatistics =
      new("match_getClubLeagueStatistics", "Check league stats", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetLeagueTable =
      new("match_getLeagueTable", "View league table", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetGroupTable =
      new("match_getGroupTable", "View group table", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetMatchBettingOddsHistory =
      new("match_getMatchBettingOddsHistory", "Review odds movement", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetClubRollingPerformance =
      new("match_getClubRollingPerformance", "Review recent performance", AgentToolCategory.Match);

    public static readonly AgentToolDefinition SaveMatchAnalysis =
      new("match_saveMatchAnalysisAsync", "Save match research", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetMatchResearchText =
      new("match_getMatchResearchTextAsync", "Read saved research", AgentToolCategory.Match);

    public static readonly AgentToolDefinition GetUpcomingMatches =
      new("match_getAvailableMatchesAsync", "Browse upcoming matches", AgentToolCategory.Match);
  }

  public static class Betting
  {
    public static readonly AgentToolDefinition GetAvailableMatches =
      new("betting_getAvailableMatches", "Browse bettable matches", AgentToolCategory.Betting);

    public static readonly AgentToolDefinition GetCurrentOdds =
      new("betting_getCurrentOdds", "Check current odds", AgentToolCategory.Betting);

    public static readonly AgentToolDefinition GetMatchAnalysis =
      new("betting_getMatchAnalysis", "Read match analysis", AgentToolCategory.Betting);

    public static readonly AgentToolDefinition PlaceBetSlip =
      new("betting_placeBetSlip", "Place a bet", AgentToolCategory.Betting);

    public static readonly AgentToolDefinition GetBetSlips =
      new("betting_getBetSlips", "Review bet slips", AgentToolCategory.Betting);

    public static readonly AgentToolDefinition GetBetSlipsAwaitingReflection =
      new("betting_getBetSlipsAwaitingReflectionAsync", "Find slips to reflect on", AgentToolCategory.Betting);
  }

  public static class SocialMedia
  {
    public static readonly AgentToolDefinition CreateXPost =
      new("socialmedia_createXPost", "Post on X", AgentToolCategory.SocialMedia);
  }

  public static class DailySlip
  {
    public static readonly AgentToolDefinition PlaceBetSlip =
      new("dailyslip_placeBetSlip", "Place a daily paper slip", AgentToolCategory.DailySlip);
  }

  public static class ResearchBet
  {
    public static readonly AgentToolDefinition GetMatchBasicInfo =
      new("researchbet_getMatchBasicInfo", "Review match details", AgentToolCategory.ResearchBet, UsesSessionMatch: true);

    public static readonly AgentToolDefinition GetMatchEvents =
      new("researchbet_getMatchEvents", "Browse betting markets", AgentToolCategory.ResearchBet, UsesSessionMatch: true);

    public static readonly AgentToolDefinition PlaceBetSlip =
      new("researchbet_placeBetSlip", "Place paper bet", AgentToolCategory.ResearchBet);
  }

  public static class Todo
  {
    public static readonly AgentToolDefinition Add =
      new("todos_add", "Add to-do items", AgentToolCategory.Todo);

    public static readonly AgentToolDefinition Complete =
      new("todos_complete", "Mark to-dos done", AgentToolCategory.Todo);

    public static readonly AgentToolDefinition Remove =
      new("todos_remove", "Remove to-dos", AgentToolCategory.Todo);

    public static readonly AgentToolDefinition GetRemaining =
      new("todos_get_remaining", "Check remaining to-dos", AgentToolCategory.Todo);

    public static readonly AgentToolDefinition GetAll =
      new("todos_get_all", "Review to-do list", AgentToolCategory.Todo);
  }

  public static class Bankroll
  {
    public static readonly AgentToolDefinition GetBalance =
      new("bankroll_getBalance", "Check balance", AgentToolCategory.Bankroll);

    public static readonly AgentToolDefinition GetDaysUntilPayday =
      new("bankroll_getDaysUntillPayday", "Check days until payday", AgentToolCategory.Bankroll);
  }

  public static class WebSearch
  {
    public static readonly AgentToolDefinition SearchNews =
      new("websearch_searchNews", "Search news", AgentToolCategory.WebSearch);

    public static readonly AgentToolDefinition GetWebGrounding =
      new("websearch_getWebGrounding", "Research on the web", AgentToolCategory.WebSearch);
  }

  public static class Memories
  {
    public static readonly AgentToolDefinition GetRecords =
      new("memories_getRecords", "List saved memories", AgentToolCategory.Memories);

    public static readonly AgentToolDefinition Read =
      new("memories_read", "Open memory note", AgentToolCategory.Memories);

    public static readonly AgentToolDefinition Write =
      new("memories_write", "Save memory note", AgentToolCategory.Memories);

    public static readonly AgentToolDefinition Append =
      new("memories_append", "Add to memory note", AgentToolCategory.Memories);

    public static readonly AgentToolDefinition Replace =
      new("memories_replace", "Edit memory note", AgentToolCategory.Memories);

    public static readonly AgentToolDefinition Delete =
      new("memories_delete", "Delete memory note", AgentToolCategory.Memories);
  }

  public static IReadOnlyList<AgentToolDefinition> All { get; } =
  [
    Match.GetLineups,
    Match.GetInjuries,
    Match.GetHead2HeadStats,
    Match.GetClubDailySummary,
    Match.GetClubRecentGames,
    Match.GetClubLeagueStatistics,
    Match.GetLeagueTable,
    Match.GetGroupTable,
    Match.GetMatchBettingOddsHistory,
    Match.GetClubRollingPerformance,
    Match.SaveMatchAnalysis,
    Match.GetMatchResearchText,
    Match.GetUpcomingMatches,
    Betting.GetAvailableMatches,
    Betting.GetCurrentOdds,
    Betting.GetMatchAnalysis,
    Betting.PlaceBetSlip,
    Betting.GetBetSlips,
    Betting.GetBetSlipsAwaitingReflection,
    DailySlip.PlaceBetSlip,
    SocialMedia.CreateXPost,
    ResearchBet.GetMatchBasicInfo,
    ResearchBet.GetMatchEvents,
    ResearchBet.PlaceBetSlip,
    Todo.Add,
    Todo.Complete,
    Todo.Remove,
    Todo.GetRemaining,
    Todo.GetAll,
    Bankroll.GetBalance,
    Bankroll.GetDaysUntilPayday,
    WebSearch.SearchNews,
    WebSearch.GetWebGrounding,
    Memories.GetRecords,
    Memories.Read,
    Memories.Write,
    Memories.Append,
    Memories.Replace,
    Memories.Delete,
  ];
}
