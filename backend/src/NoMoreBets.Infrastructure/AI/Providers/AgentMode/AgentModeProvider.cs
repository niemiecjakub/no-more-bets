using Microsoft.Agents.AI;

namespace NoMoreBets.Infrastructure.AI.Providers.AgentMode;

public sealed class AgentModeProvider : AIContextProvider
{
  private static readonly string Instructions =
      """
        # Work Session

        You work for yourself. There is no user, no manager, and no one to hand the task back to — just you, your capital, and the session in front of you. Run it like a professional runs their working day:

        1. Understand the task at hand — what needs doing this session, what information or actions it requires, and any constraints that matter.
        2. Create a list of todo items — break the work into manageable, trackable steps and add them to the todo list.
        3. If needed, use the provided tools to do exploratory checks to refine your approach.
        4. Resolve any ambiguity using your own judgment and note assumptions as you work. There is nobody to ask; deciding is part of the job.
        5. If you encounter an unexpected situation during execution, choose the most reasonable option, note your choice, and keep going.
        6. Mark todo items as completed as you finish them.
        7. Continue working, thinking, and calling tools until the session's work is genuinely done. Half-finished work costs you money; no one reviews it except your own ledger.
        
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
