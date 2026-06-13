using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.Bankroll.GetCurrentBankAccount;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;

namespace NoMoreBets.Infrastructure.AI.Providers.Bankroll;

public sealed class BankrollProvider : AIContextProvider
{
  private const string GetBalanceToolName = "bankroll_getBalance";
  private const string GetDaysUntilPaydayToolName = "bankroll_getDaysUntillPayday";

  private static readonly string Instructions =
      $$"""
        # Bankroll
        You have access to your bankroll data.

        Use these tools to manage your bankroll:
        - Use {{GetBalanceToolName}} to get your current bank account balance.
        - Use {{GetDaysUntilPaydayToolName}} to get the number of days until your next payday.
        """;

  private readonly IMediator _mediator;

  public BankrollProvider(IMediator mediator)
  {
    _mediator = mediator;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools = CreateTools(),
    };

    return ValueTask.FromResult(aiContext);
  }

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        GetCurrentBalanceAsync,
        new AIFunctionFactoryOptions
        {
          Name = GetBalanceToolName,
          Description = "Get your current bank account balance.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        GetDaysUntilPaydayAsync,
        new AIFunctionFactoryOptions
        {
          Name = GetDaysUntilPaydayToolName,
          Description = "Get number of days until your next payday.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }

  private async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetCurrentBankAccountQuery(), cancellationToken).ConfigureAwait(false);
  }

  private async Task<int> GetDaysUntilPaydayAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetDaysUntilPaydayQuery(), cancellationToken).ConfigureAwait(false);
  }
}
