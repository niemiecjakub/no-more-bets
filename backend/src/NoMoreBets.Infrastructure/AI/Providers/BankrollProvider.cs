using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.Bankroll.GetCurrentBankAccount;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Providers;
public class BankrollProvider : AIContextProvider
{
  private readonly IMediator _mediator;

  public BankrollProvider(IMediator mediator)
  {
    _mediator = mediator;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = """
        ## Bankroll
        You have access to your bankroll data via the `Bankroll_*` tools.
        """,
      Tools = CreateTools()
    };

    return ValueTask.FromResult(aiContext);
  }

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;
    return
    [
      AIFunctionFactory.Create(this.GetCurrentBalanceAsync, new AIFunctionFactoryOptions { Name = "Bankroll_GetBalance", SerializerOptions = serializerOptions }),
      AIFunctionFactory.Create(this.GetDaysUntilPaydayAsync, new AIFunctionFactoryOptions { Name = "Bankroll_GetDaysUntillPayday", SerializerOptions = serializerOptions }),
     ];
  }

  [Description("Get your current bank account balance.")]
  public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetCurrentBankAccountQuery(), cancellationToken).ConfigureAwait(false);
  }

  [Description("Get number of days until your next payday.")]
  public async Task<int> GetDaysUntilPaydayAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetDaysUntilPaydayQuery(), cancellationToken).ConfigureAwait(false);
  }

}
