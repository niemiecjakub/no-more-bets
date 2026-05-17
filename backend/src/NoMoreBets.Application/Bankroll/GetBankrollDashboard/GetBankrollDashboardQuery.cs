using MediatR;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Bankroll.GetBankrollDashboard;

public record GetBankrollDashboardQuery : IRequest<BankrollDashboardDto>;

public record BankrollDashboardDto(
  decimal CurrentBalance,
  int DaysUntilPayday,
  IReadOnlyList<BankrollRecordDto> Records);

public record BankrollRecordDto(
  int Id,
  string Name,
  decimal Amount,
  string Flow,
  int? BetId,
  DateTime CreatedAt);

public sealed class GetBankrollDashboardHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetBankrollDashboardQuery, BankrollDashboardDto>
{
  public async Task<BankrollDashboardDto> Handle(
    GetBankrollDashboardQuery request,
    CancellationToken cancellationToken)
  {
    var daysTask = mediator.Send(new GetDaysUntilPaydayQuery(), cancellationToken);

    // Same DbContext: do not run bankroll queries concurrently (EF Core concurrency guard).
    var balance = await unitOfWork.Bankroll
      .GetCurrentBalanceAsync(cancellationToken)
      .ConfigureAwait(false);
    var entities = await unitOfWork.Bankroll
      .GetAllOrderedByCreatedAtDescAsync(cancellationToken)
      .ConfigureAwait(false);
    var days = await daysTask.ConfigureAwait(false);

    var records = entities
      .Select(r => new BankrollRecordDto(
        r.Id,
        r.Name,
        r.Amount,
        r.Direction == BankrollFlow.In ? nameof(BankrollFlow.In) : nameof(BankrollFlow.Out),
        r.BetId,
        r.CreatedAt))
      .ToList();

    return new BankrollDashboardDto(balance, days, records);
  }
}
