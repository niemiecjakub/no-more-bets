using Microsoft.Agents.AI;

namespace NoMoreBets.Infrastructure.AI.Providers.AgentMode;

public sealed class AgenticModeProvider : AIContextProvider
{
  private static readonly string Instructions =
      """
        Autonomy: Complete this session end-to-end with the tools provided. There is no user to hand work back to — decide from evidence and note assumptions.

        Stop when the task success criteria are met. Do not stop with partial work unless required evidence is unavailable — then name what is missing.
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
