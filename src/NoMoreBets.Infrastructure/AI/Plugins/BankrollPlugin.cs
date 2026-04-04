using System.ComponentModel;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class BankrollPlugin
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;

  public BankrollPlugin(IUnitOfWork unitOfWork, IMediator mediator)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
  }

  [KernelFunction("GetCurrentBalance")]
  [Description("Returns the current bankroll balance: sum of all IN amounts minus sum of all OUT amounts.")]
  public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    return await _unitOfWork.Bankroll.GetCurrentBalanceAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetDaysUntilPayday")]
  [Description("Returns the number of whole calendar days until the next payday. Paydays are the last day of each month (UTC); on month-end, the next payday is the last day of the following month.")]
  public async Task<int> GetDaysUntilPaydayAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetDaysUntilPaydayQuery(), cancellationToken).ConfigureAwait(false);
  }
}
