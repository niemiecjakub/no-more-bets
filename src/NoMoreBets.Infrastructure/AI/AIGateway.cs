using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Infrastructure.AI;

public sealed class AIGateway(IPluginFactory pluginFactory, IOptions<OpenAIOptions> openAiOptions) : IMatchPrediction
{
  private Kernel CreateKernel()
  {
    var builder = Kernel.CreateBuilder();
    var openAi = openAiOptions.Value;
    string modelId = openAi.ModelId;
    string apiKey = openAi.ApiKey;
    builder.AddOpenAIChatCompletion(modelId, apiKey);
    return builder.Build();
  }

  public async Task<string> InvokeAsync(MatchPredictionPromptRequest request, CancellationToken cancellationToken = default)
  {
    Kernel kernel = CreateKernel();
    var matchPlugin = await pluginFactory.CreateMatchPluginAsync(request.Match.Id, cancellationToken).ConfigureAwait(false);
    kernel.Plugins.AddFromObject(matchPlugin);

    var searchPlugin = pluginFactory.CreateSearchPlugin();
    kernel.Plugins.AddFromObject(searchPlugin);

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
      ResponseFormat = typeof(StructuredMatchAnalysis),
      ChatSystemPrompt = SystemPrompt
    };

    string query = $"""
      MATCH INFORMATION:

      {request.Match.HomeClub.Name} vs {request.Match.AwayClub.Name}. Date: {request.Match.MatchDate:yyyy-MM-dd HH:mm} UTC. 
      Home Club: {request.Match.HomeClub.Name} (ID = {request.Match.HomeClub.Id})  
      Away Club: {request.Match.AwayClub.Name} (ID = {request.Match.AwayClub.Id})  
      """;

    var arguments = new KernelArguments(executionSettings);
    var result = await kernel.InvokePromptAsync(query, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    return result.ToString() ?? string.Empty;
  }

  const string SystemPrompt = """
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
}
