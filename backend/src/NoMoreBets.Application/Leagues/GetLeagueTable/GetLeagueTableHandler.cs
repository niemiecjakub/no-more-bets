using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.GetLeagueTable;

public record GetLeagueTableQuery(int LeagueId, DateOnly? AsOfDate = null) : IRequest<IReadOnlyList<LeagueTableStanding>?>;

public sealed class GetLeagueTableHandler(IUnitOfWork unitOfWork, ILogger<GetLeagueTableHandler>? logger = null) : IRequestHandler<GetLeagueTableQuery, IReadOnlyList<LeagueTableStanding>?>
{
  public async Task<IReadOnlyList<LeagueTableStanding>?> Handle(GetLeagueTableQuery request, CancellationToken cancellationToken)
  {
    var standings = await unitOfWork.Leagues.GetLeagueTableAsOfAsync(request.LeagueId, request.AsOfDate, cancellationToken).ConfigureAwait(false);
    if (standings == null || standings.Count == 0)
    {
      logger?.LogWarning("No league table found for league {LeagueId} up to date {AsOfDate}.", request.LeagueId, request.AsOfDate);
    }

    return standings;
  }
}
