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
  [Description("Returns bank account balance")]
  public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    return await _unitOfWork.Bankroll.GetCurrentBalanceAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetDaysUntilPayday")]
  [Description("Returns number of days untill next payday.")]
  public async Task<int> GetDaysUntilPaydayAsync(CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetDaysUntilPaydayQuery(), cancellationToken).ConfigureAwait(false);
  }
}
