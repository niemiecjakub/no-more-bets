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

    var plugin = pluginFactory.CreateMatchPlugin(command.MatchId);
    kernel.Plugins.AddFromObject(plugin);

    string homeName = match.HomeClub?.Name ?? "Home";
    string awayName = match.AwayClub?.Name ?? "Away";

    int homeClubId = match.HomeClubId;
    int awayClubId = match.AwayClubId;

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
      ResponseFormat = typeof(StructuredMatchAnalysis),
      ReasoningEffort = "medium",
      ChatSystemPrompt = prompt
    };

    string query = $$$"""
      MATCH INFORMATION:

      {{{homeName}}} vs {{{awayName}}}. Date: {{{match.MatchDate:yyyy-MM-dd HH:mm}}} UTC. 
      Home Club: {{$homeClub}} (ID = {{$homeClubId}})  
      Away Club: {{$awayClub}} (ID = {{$awayClubId}})  
      """;

    var arguments = new KernelArguments(executionSettings);

    logger.LogInformation("Starting match prediction for MatchId {MatchId}: {HomeName} vs {AwayName}", command.MatchId, homeName, awayName);

    var kernelClone = kernel.Clone();
    var result = await kernelClone.InvokePromptAsync(prompt, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    var restultStr = result.ToString() ?? string.Empty;

    var analysis = new MatchAnalysis
    {
      MatchId = command.MatchId,
      Code = "gpt-5.1 - medium",
      Content = restultStr
    };

    await unitOfWork.Matches.AddMatchAnalysisAsync(analysis, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Match prediction completed for MatchId {MatchId}.", command.MatchId);
    return Unit.Value;
  }

  const string prompt = """
    You are a football intelligence analyst producing pre-match reports for a professional betting and analytics platform.
    
    Your reports must be evidence-based, clearly structured, and free of unsupported claims.
    Your goal is to produce a **structured pre-match analysis** strictly based on verified football data retrieved from available plugin functions. 
    You **must** retrieve and interpret all data before producing the final analysis.
    
    # WORKFLOW

    ## STEP 1 — DATA RETRIEVAL
    Retrieve the following using plugin functions:
    
    • Match lineups  
    • Injuries and unavailable players  
    • Recent matches for both clubs  
    • Rolling team performance and player ratings  
    • League statistics (table position, xG, xGA, xPts)  
    • Historical head-to-head statistics  
    • Betting odds history and market movements  
    • Club daily summaries  
    • Match preview  
    
    Do not begin analysis until all relevant data has been retrieved.
    
    ## STEP 2 — DATA INTERPRETATION
    From the retrieved data, identify key signals:
    
    • **Form & Momentum** 
    • **Tactical Structure** 
    • **Squad Availability**
    • **Statistical Edge** 
    • **Betting Market Signals**
    
    ## STEP 3 — SYNTHESIS

    Using the interpreted signals, produce a concise, professional analysis that covers:
    
    • Which team has stronger form and momentum  
    • Tactical matchups and key pitch zones  
    • Important players influencing the game  
    • Statistical advantages or weaknesses  
    • Market expectations and betting insights  
    • How all factors may influence match dynamics
    
    # ANALYSIS GUIDELINES

    • Use **only retrieved data**; do not invent players, stats, or injuries.  
    • Avoid unsupported speculation; prefer evidence-based observations.  
    • Relate statistics, tactics, and form clearly.  
    • Highlight both advantages and risks for each team.  
    • Keep paragraphs concise and focused; use bullet points where appropriate for clarity.  
    """;
}
