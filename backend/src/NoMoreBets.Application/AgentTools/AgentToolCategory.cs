namespace NoMoreBets.Application.AgentTools;

public enum AgentToolCategory
{
  Match,
  Betting,
  ResearchBet,
  SocialMedia,
  Todo,
  Bankroll,
  WebSearch,
  Memories,
  DailySlip,
}

public static class AgentToolCategoryExtensions
{
  public static string ToSlug(this AgentToolCategory category) => category switch
  {
    AgentToolCategory.Match => "match",
    AgentToolCategory.Betting => "betting",
    AgentToolCategory.ResearchBet => "researchbet",
    AgentToolCategory.SocialMedia => "socialmedia",
    AgentToolCategory.Todo => "todo",
    AgentToolCategory.Bankroll => "bankroll",
    AgentToolCategory.WebSearch => "websearch",
    AgentToolCategory.Memories => "memories",
    AgentToolCategory.DailySlip => "dailyslip",
    _ => "unknown",
  };
}
