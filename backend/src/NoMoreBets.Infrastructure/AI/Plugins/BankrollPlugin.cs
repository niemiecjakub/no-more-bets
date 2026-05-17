using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class BankrollPlugin
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;
  private readonly ILogger<BankrollPlugin> _logger;

  public BankrollPlugin(IUnitOfWork unitOfWork, IMediator mediator, ILogger<BankrollPlugin>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
    _logger = logger ?? NullLogger<BankrollPlugin>.Instance;
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
