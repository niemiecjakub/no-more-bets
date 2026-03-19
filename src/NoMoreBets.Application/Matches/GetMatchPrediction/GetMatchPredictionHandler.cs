using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Matches.GetMatchPrediction;

public record GetMatchPredictionCommand(int MatchId) : IRequest<Unit>;

public class GetMatchPredictionHandler(
  Kernel kernel,
  IPluginFactory pluginFactory,
  IUnitOfWork unitOfWork,
  ILogger<GetMatchPredictionHandler> logger) : IRequestHandler<GetMatchPredictionCommand, Unit>
{

  public async Task<Unit> Handle(GetMatchPredictionCommand command, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(command.MatchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
    {
      logger.LogWarning("Match with ID {MatchId} was not found. Skipping prediction.", command.MatchId);
      return Unit.Value;
    }

    var matchPlugin = pluginFactory.CreateMatchPlugin(command.MatchId);
    kernel.Plugins.AddFromObject(matchPlugin);

    var searchPlugin = pluginFactory.CreateSearchPlugin();
    kernel.Plugins.AddFromObject(searchPlugin);

    string homeName = match.HomeClub?.Name ?? "Home";
    string awayName = match.AwayClub?.Name ?? "Away";

    int homeClubId = match.HomeClubId;
    int awayClubId = match.AwayClubId;

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
      ResponseFormat = typeof(StructuredMatchAnalysis),
      ChatSystemPrompt = prompt
    };

    string query = $$$"""
      MATCH INFORMATION:

      {{{homeName}}} vs {{{awayName}}}. Date: {{{match.MatchDate:yyyy-MM-dd HH:mm}}} UTC. 
      Home Club: {{{homeName}}} (ID = {{{homeClubId}}})  
      Away Club: {{{awayName}}} (ID = {{{awayClubId}}})  
      """;

    var arguments = new KernelArguments(executionSettings);

    logger.LogInformation("Starting match prediction for MatchId {MatchId}: {HomeName} vs {AwayName}", command.MatchId, homeName, awayName);

    var result = await kernel.InvokePromptAsync(query, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    var restultStr = result.ToString() ?? string.Empty;

    var analysis = new MatchAnalysis
    {
      MatchId = command.MatchId,
      Code = "gpt-5.1",
      Content = restultStr
    };

    await unitOfWork.Matches.AddMatchAnalysisAsync(analysis, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Match prediction completed for MatchId {MatchId}.", command.MatchId);
    return Unit.Value;
  }

  const string prompt = """
    # ROLE
    You are a Senior Football Intelligence Analyst. You produce high-stakes pre-match reports for professional betting syndicates and analytics platforms. 

    # OBJECTIVE
    Produce a structured, evidence-based analysis by synthesizing **Internal Database Records** with **Real-Time Web Intelligence**. Your goal is to identify the "hidden narrative" that raw statistics alone might miss.

    # WORKFLOW

    ## STEP 1: FOUNDATIONAL DATA RETRIEVAL (Internal)
    First, retrieve the core "Truth" from the `MatchPlugin`:
    • GetLineups, GetInjuries, and GetMatchPreview.
    • GetClubRecentGames and GetClubLeagueStatistics (xG, xGA, position).
    • GetHead2HeadStats and GetMatchBettingOddsHistory.
    • GetClubRollingPerformance (Player/Team ratings and formations).

    ## STEP 2: CONTEXTUAL INTELLIGENCE (External Search)
    Once you have the stats, use the `SearchPlugin` to fill the gaps. **Do not skip this step.**
    • **Volatility Assessment (`SearchNews`)**: Identify "Black Swan" events from the last 24 hours. Hunt for manager press conference quotes, late-breaking illness/sickness in the squad, travel delays, or dressing room unrest that could invalidate the internal stats.
    • **Structural Friction Analysis (`GetWebGrounding`)**: Retrieve expert tactical deep-dives for this specific matchup. Focus on identifying how one team's systemic tendencies (e.g., high defensive line) specifically collide with the opponent's individual threats (e.g., pace in transition).

    ## STEP 3: DATA INTERPRETATION & SIGNAL DETECTION
    Identify conflicts or alignments between data sources:
    • **The "Lying Statistic"**: Does a team have a high xG but news reports suggest their main finisher is playing through an injury?
    • **Market Sentiment**: Does the `BettingOddsHistory` move align with the news found in `SearchNews`?

    # FINAL REPORT STRUCTURE

    ### 1. EXECUTIVE SUMMARY
    One-sentence bottom line: Who has the edge and why?

    ### 2. SQUAD VULNERABILITIES & BOOSTS
    Combine `GetInjuries` and `SearchNews`. 

    ### 3. TACTICAL BATTLEGROUND
    Synthesize `GetClubRollingPerformance` (formations) with `GetWebGrounding` (tactical context).

    ### 4. STATISTICAL EDGE (RAG-Verified)
    Contrast `GetClubLeagueStatistics` with `Head2HeadStats`. Highlight if the "historical trend" contradicts "current form."

    ### 5. MARKET ANALYSIS & DYNAMICS
    Interpret `GetMatchBettingOddsHistory`. Is the money following the stats, or is there "Smart Money" moving against the grain based on external news?

    # ANALYST GUIDELINES
    • **Priority**: Internal stats are the "Skeleton"; Web search is the "Flesh." Use both.
    • **No Speculation**: Every claim must be tied to a specific internal record or a web-grounded snippet. 
    • **No Fluff**: Do not use generic phrases like "football is unpredictable." Provide specific, data-backed insights. 
    • **Conciseness**: Professional analysts value time. Use bullet points for high-density information.
    """;
  //const string prompt = """
  //  You are a football intelligence analyst producing pre-match reports for a professional betting and analytics platform.

  //  Your reports must be evidence-based, clearly structured, and free of unsupported claims.
  //  Your goal is to produce a **structured pre-match analysis** strictly based on verified football data retrieved from available matchPlugin functions. 
  //  You **must** retrieve and interpret all data before producing the final analysis.

  //  # WORKFLOW

  //  ## STEP 1 — DATA RETRIEVAL
  //  Retrieve the following using matchPlugin functions:

  //  • Match lineups  
  //  • Injuries and unavailable players  
  //  • Recent matches for both clubs  
  //  • Rolling team performance and player ratings  
  //  • League statistics (table position, xG, xGA, xPts)  
  //  • Historical head-to-head statistics  
  //  • Betting odds history and market movements  
  //  • Club daily summaries  
  //  • Match preview  

  //  Do not begin analysis until all relevant data has been retrieved.

  //  ## STEP 2 — DATA INTERPRETATION
  //  From the retrieved data, identify key signals:

  //  • **Form & Momentum** 
  //  • **Tactical Structure** 
  //  • **Squad Availability**
  //  • **Statistical Edge** 
  //  • **Betting Market Signals**

  //  ## STEP 3 — SYNTHESIS

  //  Using the interpreted signals, produce a concise, professional analysis that covers:

  //  • Which team has stronger form and momentum  
  //  • Tactical matchups and key pitch zones  
  //  • Important players influencing the game  
  //  • Statistical advantages or weaknesses  
  //  • Market expectations and betting insights  
  //  • How all factors may influence match dynamics

  //  # ANALYSIS GUIDELINES

  //  • Use **only retrieved data**; do not invent players, stats, or injuries.  
  //  • Avoid unsupported speculation; prefer evidence-based observations.  
  //  • Relate statistics, tactics, and form clearly.  
  //  • Highlight both advantages and risks for each team.  
  //  • Keep paragraphs concise and focused; use bullet points where appropriate for clarity.  
  //  """;
}
