using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class BankrollPlugin
{
  private readonly IUnitOfWork _unitOfWork;

  public BankrollPlugin(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
  }

  [KernelFunction("GetCurrentBalance")]
  [Description("Returns the current bankroll balance: sum of all IN amounts minus sum of all OUT amounts.")]
  public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    return await _unitOfWork.Bankroll.GetCurrentBalanceAsync(cancellationToken).ConfigureAwait(false);
  }
}
