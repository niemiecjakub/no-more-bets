using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchPrediction;

public record GetMatchPredictionCommand(int MatchId) : IRequest<string>;

public class GetMatchPredictionHandler(
  Kernel kernel,
  IPluginFactory pluginFactory,
  IUnitOfWork unitOfWork,
  ILogger<GetMatchPredictionHandler> logger) : IRequestHandler<GetMatchPredictionCommand, string>
{

  public async Task<string> Handle(GetMatchPredictionCommand command, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(command.MatchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return $"Match with ID {command.MatchId} was not found.";

    var plugin = pluginFactory.CreateMatchPlugin(command.MatchId);
    kernel.Plugins.AddFromObject(plugin);

    int homeClubId = match.HomeClubId;
    int awayClubId = match.AwayClubId;
    string homeName = match.HomeClub?.Name ?? "Home";
    string awayName = match.AwayClub?.Name ?? "Away";

    //const string preGameAnalysisPrompt = """
    //You are a professional football tactical analyst and sharp bettor. 
    //Your goal is to produce a comprehensive Pre-Game Intelligence Report for: {{$matchInfo}}.

    //### MANDATORY DATA COLLECTION
    //Before reasoning, you MUST use the provided tools to retrieve:
    //1. Tactical Context: Match preview and recent daily summaries (homeId: {{$homeClubId}}, awayId: {{$awayClubId}}).
    //2. Personnel: Confirmed or projected lineups and the current injury/suspension list.
    //3. Performance: Recent form (last 5 games) and historical Head-to-Head patterns.
    //4. Market Sentiment: Current betting odds and historical odds movement.

    //### ANALYSIS REQUIREMENTS
    //Using ONLY the gathered evidence, structure your analysis as follows:

    //1. TACTICAL MATCH-UP: Identify the "battleground." Based on lineups and previews, how will these teams clash? Mention key player absences and their likely impact on the team's style.
    //2. MOMENTUM & TRENDS: Contrast the recent form and H2H data. Is one team over-performing their underlying stats or historical trend?
    //3. MARKET ANALYSIS: Compare your tactical findings against the 'MatchBettingOddsHistory'. 
    //   - Does the market (odds) accurately reflect the injury news and form? 
    //   - Identify if the odds are 'drifting' (getting higher) or 'shortening' (getting lower) and what that implies.
    //4. BETTING VERDICT: 
    //   - Primary Angle: (e.g., Home Win, Over/Under, Asian Handicap).
    //   - Confidence Level: (Low/Medium/High) based on data completeness.
    //   - Value Note: Point out a specific market (e.g., "Away Win is mispriced given the Home Team's midfield injuries").

    //Keep the reply concise and evidence-based. Do not invent data.
    //""";

    const string preGameAnalysisPrompt = """
    ### ROLE
    You are a Senior Football Tactical Analyst and Professional Odds Trader. Your goal is to provide a "Sharps-level" pre-game report for the following match: {{$matchInfo}}.

    ### MANDATORY WORKFLOW
    You must execute your analysis in the following sequence:
    1. DATA RETRIEVAL: Use your tools to fetch:
       - Lineups and Injuries (Check for key absences).
       - League Statistics (Focus on xG, xGA, and xPts performance).
       - Head-to-Head and Recent Games (Look for momentum and historical patterns).
       - Match Preview and Daily Summaries (For qualitative news/tactical shifts).
       - Betting Odds History (Analyze price movement/market sentiment).

    2. CROSS-REFERENCING: Compare qualitative news (Injuries/Daily Summaries) against quantitative data (xG/League Stats).

    3. MARKET VALIDATION: Determine if the current 'MarketPriceHistory' accurately reflects the tactical reality you discovered.

    ### ANALYSIS GUIDELINES
    - NO HALLUCINATIONS: If a tool returns null or "No data available," state that clearly. Do not invent scores or players.
    - XG INTERPRETATION: Use xPtsDiff and xGDiff to identify if a team is "lucky" (over-performing) or "unlucky" (under-performing).
    - MARKET SENTIMENT: Use the 'OddsTimeline'. If a price is shortening (dropping), explain why based on the data (e.g., a star player is starting).

    ### OUTPUT STRUCTURE
    Your report must follow this Markdown format:

    # Pre-Game Intelligence: [Home Team] vs [Away Team]

    ## 1. Tactical Personnel Report
    - **Impactful Absences:** List injuries and how they disrupt the team's system.
    - **Expected Setup:** Brief tactical expectation based on lineups.

    ## 2. Performance & Form Analysis
    - **Statistical Profile:** Compare xG/xGA for both teams. Who is more efficient?
    - **Momentum:** Summarize the last 5 games and H2H context.

    ## 3. Market Sentiment & Value
    - **Odds Movement:** Describe if the market is moving toward the Home or Away side and why.
    - **The "Sharps" Angle:** Identify any "mispricing" where the data contradicts the odds.

    ## 4. Final Betting Verdict
    - **Primary Recommendation:** (e.g., Home Win, Over 2.5, etc.)
    - **Confidence Level:** [Low / Medium / High]
    - **Key Trigger:** One sentence summarizing the main reason for this pick.

    Use these IDs for tool calls: HomeID: {{$homeClubId}}, AwayID: {{$awayClubId}}.
    """;

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
    };

    var arguments = new KernelArguments(executionSettings)
    {
      ["matchInfo"] = $"{homeName} vs {awayName}. Date: {match.MatchDate:yyyy-MM-dd HH:mm} UTC.",
      ["homeClubId"] = homeClubId,
      ["awayClubId"] = awayClubId
    };

    var result = await kernel.InvokePromptAsync(preGameAnalysisPrompt, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    return result.ToString() ?? string.Empty;
  }
}
