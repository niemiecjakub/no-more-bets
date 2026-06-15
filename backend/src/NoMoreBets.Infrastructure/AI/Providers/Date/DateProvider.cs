using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace NoMoreBets.Infrastructure.AI.Providers.Date;

public sealed class DateProvider : AIContextProvider
{
  protected override ValueTask<AIContext> ProvideAIContextAsync(
    InvokingContext context,
    CancellationToken cancellationToken = default)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var aiContext = new AIContext
    {
      Instructions = $"""
        # Date
        Today is {today}.

        """,
    };

    return ValueTask.FromResult(aiContext);
  }
}
