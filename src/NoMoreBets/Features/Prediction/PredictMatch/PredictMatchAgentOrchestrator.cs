using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using NoMoreBets.Features.Prediction.Plugins;

namespace NoMoreBets.Features.Prediction.PredictMatch;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
/// <summary>
/// Runs the multi-agent chat for match prediction.
/// </summary>
public sealed class PredictMatchAgentOrchestrator(
    FootballDataPlugin footballDataPlugin,
    SquadPlugin squadPlugin,
    BookmakerPlugin bookmakerPlugin,
    IOptions<OpenAiAgentOptions> openAiOptions) : IPredictMatchAgentOrchestrator
{

  private const string APPROVE_KEYWORD = "##APPROVED##";
  public async Task<PredictMatchResult> RunAsync(PredictMatchQuery query, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(openAiOptions.Value.ApiKey))
    {
      return new PredictMatchResult
      {
        BettingTicket = string.Empty,
        Transcript = []
      };
    }

    var quantKernel = CreateKernelBuilder().Build();
    quantKernel.Plugins.AddFromObject(footballDataPlugin, "FootballDataPlugin");

    var scoutKernel = CreateKernelBuilder().Build();
    scoutKernel.Plugins.AddFromObject(squadPlugin, "SquadPlugin");

    var bookieKernel = CreateKernelBuilder().Build();
    bookieKernel.Plugins.AddFromObject(bookmakerPlugin, "BookmakerPlugin");

    var reviewerKernel = CreateKernelBuilder().Build();

    var quantAgent = CreateQuantAgent(quantKernel);
    var scoutAgent = CreateScoutAgent(scoutKernel);
    var bookieAgent = CreateBookieAgent(bookieKernel);
    var reviewerAgent = CreateReviewerAgent(reviewerKernel);

    KernelFunction terminateFunction = KernelFunctionFactory.CreateFromPrompt(
      $$$"""
      Determine if the bookmaker ticket has been approved. If so, respond with a single word: approved.

      History:

      {{$history}}
      """);

    KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(
      $$$"""
      Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
      State only the name of the participant to take the next turn.

      Choose only from these participants:
      - {{{quantAgent.Name}}}
      - {{{scoutAgent.Name}}}
      - {{{bookieAgent.Name}}}
      - {{{reviewerAgent.Name}}}

      Always follow these steps when selecting the next participant:
      1) After user input, it is {{{quantAgent.Name}}}'s turn.
      2) After {{{quantAgent.Name}}} replies, it's {{{scoutAgent.Name}}}'s turn.
      3) After {{{scoutAgent.Name}}} replies, it's {{{bookieAgent.Name}}}'s turn to create a ticket.
      4) After {{{bookieAgent.Name}}} replies, it's {{{reviewerAgent.Name}}}'s to reflect on the analysis and ticket.
      5) If the ticket is approved, the conversation ends.
      6) If the ticket isn't approved, keep selecting next spearkers base on the needs.

      History:
      {{$history}}
      """);

    var chat = new AgentGroupChat(quantAgent, scoutAgent, bookieAgent, reviewerAgent)
    {
      ExecutionSettings = new AgentGroupChatSettings
      {
        SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, CreateKernelBuilder().Build())
        {
          AgentsVariableName = "agents",
          HistoryVariableName = "history",
        },
        TerminationStrategy = new KernelFunctionTerminationStrategy(terminateFunction, CreateKernelBuilder().Build())
        {
          Agents = [reviewerAgent],
          HistoryVariableName = "history",
          ResultParser = (result) => result.GetValue<string>()?.Contains(APPROVE_KEYWORD, StringComparison.OrdinalIgnoreCase) ?? false,
        },
      },
    };
    chat.AddChatMessage(
        new ChatMessageContent(
            AuthorRole.User,
            BuildUserPrompt(query)));

    var transcript = new List<PredictMatchAgentMessage>();
    await foreach (var message in chat.InvokeAsync(cancellationToken))
    {
      transcript.Add(new PredictMatchAgentMessage(message.AuthorName ?? "Unknown", message.Content ?? string.Empty));
      Console.WriteLine();
      Console.WriteLine($"# {message.Role} - {message.AuthorName ?? "*"}: '{message.Content}'");
      Console.WriteLine();
    }

    var ticket = transcript.LastOrDefault(t => t.Author.Equals("BookieAgent", StringComparison.OrdinalIgnoreCase));

    return new PredictMatchResult
    {
      BettingTicket = ticket?.Content ?? "None",
      Transcript = transcript
    };
  }

  private IKernelBuilder CreateKernelBuilder()
  {
    var builder = Kernel.CreateBuilder();
    builder.AddOpenAIChatCompletion(openAiOptions.Value.ModelId, openAiOptions.Value.ApiKey);
    return builder;
  }

  private static ChatCompletionAgent CreateQuantAgent(Kernel kernel)
  {
    return new ChatCompletionAgent
    {
      Name = "QuantAgent",
      Kernel = kernel,
      Arguments = new(new PromptExecutionSettings()
      {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
      }),
      Instructions =
            """
                You are QuantAgent.
                Focus strictly on mathematical probability, expected goals (xG), and objective data.
                Use FootballDataPlugin functions to gather league table, xG, H2H, and form context.
                Produce concise numeric reasoning and avoid narrative opinions.
                """
    };
  }

  private static ChatCompletionAgent CreateScoutAgent(Kernel kernel)
  {
    return new ChatCompletionAgent
    {
      Name = "ScoutAgent",
      Kernel = kernel,
      Arguments = new(new PromptExecutionSettings()
      {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
      }),
      Instructions =
            """
                You are ScoutAgent.
                Focus on tactical impact of missing players, injury burden, and lineup quality.
                Use SquadPlugin functions to inspect injuries and predicted lineups.
                Explain tactical implications in concise bullet points.
                """
    };
  }

  private static ChatCompletionAgent CreateBookieAgent(Kernel kernel)
  {
    return new ChatCompletionAgent
    {
      Name = "BookieAgent",
      Kernel = kernel,
      Arguments = new(new PromptExecutionSettings()
      {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
      }),
      Instructions =
            """    
                You are the BookieAgent. You translate raw analysis into actionable betting value.
                You must synthesize QuantAgent and ScoutAgent outputs and bookmaker events from BookmakerPlugin.
                """
    };
  }

  private static ChatCompletionAgent CreateReviewerAgent(Kernel kernel)
  {
    return new ChatCompletionAgent
    {
      Name = "ReviewerAgent",
      Kernel = kernel,
      Instructions =
            $"""
            You are the ReviewerAgent, the ultimate authority. 
            Your goal is to ensure the prediction is logically sound and data-driven.
            
            1. Evaluate if QuantAgent's xG/data matches ScoutAgent's lineup reality.
            2. Verify if BookieAgent's JSON ticket accurately reflects their findings.
            3. If there are contradictions (e.g., Bookie predicts a win but Scout says the star striker is out), demand a revision.
            4. Only if the logic is flawless, output the final JSON ticket and end with the token: {APPROVE_KEYWORD}.
            """
    };
  }

  private static string BuildUserPrompt(PredictMatchQuery query)
  {
    return
        $"""
             Predict match betting recomme  ndations for:
             - HomeTeam: {query.HomeTeam}
             - AwayTeam: {query.AwayTeam}
             - HomeTeamId (SoccerData): {query.HomeTeamId}
             - AwayTeamId (SoccerData): {query.AwayTeamId}
             - MatchId: {query.MatchId}
             - HomeFotmobTeamId: {(query.HomeFotmobTeamId?.ToString() ?? "not_provided")}
             - AwayFotmobTeamId: {(query.AwayFotmobTeamId?.ToString() ?? "not_provided")}
             - BookmakerGameUrl: {query.BookmakerGameUrl}

             Collaboration rules:
             - QuantAgent must call FootballDataPlugin.
             - ScoutAgent must call SquadPlugin.
             - BookieAgent must call BookmakerPlugin and output final JSON ticket.
             """;
  }
}
#pragma warning restore SKEXP0110
#pragma warning restore SKEXP0001
