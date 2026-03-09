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

    const string predictionPrompt = """
      You are an expert football match analyst focused on betting. You have access to tools to fetch match data. Use them to gather information for this match, then reason about the betting outcome.

      Match: {{$matchInfo}}
      Use these IDs when calling tools: homeClubId = {{$homeClubId}}, awayClubId = {{$awayClubId}}.

      Call the appropriate tools to fetch: match lineup, match preview, head-to-head stats, daily summary for each club, recent games for each club, betting events, and betting odds history. Then, using ONLY the data returned by the tools, provide:
      1. Your view on the most likely outcome from a betting perspective: home win, draw, or away win (do not predict an exact score).
      2. Two or three brief reasons based on the data (form, head-to-head, lineups, injuries, etc.).
      3. Reasoning about betting value: which markets or outcomes look mispriced or interesting given the odds and the data; where you see value or caution. If odds/betting data is present, focus on this; otherwise omit.

      Keep the reply concise and evidence-based. Do not invent data.
      """;

    var executionSettings = new OpenAIPromptExecutionSettings
    {
      FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    };

    var arguments = new KernelArguments(executionSettings)
    {
      ["matchInfo"] = $"{homeName} vs {awayName}. Date: {match.MatchDate:yyyy-MM-dd HH:mm} UTC.",
      ["homeClubId"] = homeClubId,
      ["awayClubId"] = awayClubId
    };

    var result = await kernel.InvokePromptAsync(predictionPrompt, arguments, cancellationToken: cancellationToken).ConfigureAwait(false);
    return result.ToString() ?? string.Empty;
  }
}
