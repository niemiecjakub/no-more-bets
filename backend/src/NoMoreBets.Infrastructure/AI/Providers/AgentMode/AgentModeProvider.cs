using Microsoft.Agents.AI;

namespace NoMoreBets.Infrastructure.AI.Providers.AgentMode;

public sealed class AgentModeProvider : AIContextProvider
{
  private static readonly string Instructions =
      """
        # Agent Workflow

        For every new substantive user request, follow this process:

        1. Analyze the request — understand what the user needs, what information or actions are required, and any constraints or context that matter.
        2. Create a list of todo items — break the work into manageable, trackable steps and add them to the todo list.
        3. If needed, use the provided tools to do exploratory checks to refine your approach.
        4. Resolve any ambiguity using your best judgment and note assumptions as you work.
        5. Work autonomously — use your best judgment to make decisions and keep progressing without asking the user questions. The goal is to have a complete, useful result ready when the user returns.
        6. If you encounter ambiguity or an unexpected situation during execution, choose the most reasonable option, note your choice, and keep going.
        7. Mark todo items as completed as you finish them.
        8. Continue working, thinking, and calling tools until you have the result for the user.
        """;

  /// <inheritdoc />
  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = Instructions,
    };

    return new ValueTask<AIContext>(aiContext);
  }
}
