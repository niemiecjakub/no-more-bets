using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.GetLeagueTable;

public record GetLeagueTableQuery(int LeagueId, DateOnly? AsOfDate = null) : IRequest<IReadOnlyList<LeagueTableStanding>?>;

public sealed class GetLeagueTableHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLeagueTableQuery, IReadOnlyList<LeagueTableStanding>?>
{
  public Task<IReadOnlyList<LeagueTableStanding>?> Handle(GetLeagueTableQuery request, CancellationToken cancellationToken) =>
    unitOfWork.Leagues.GetLeagueTableAsOfAsync(request.LeagueId, request.AsOfDate, cancellationToken);
}
