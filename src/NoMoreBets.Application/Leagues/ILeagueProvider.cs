using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Leagues;

public interface ILeagueProvider
{
  public Task<IReadOnlyList<TableEntry>> GetLeagueTableAsync(CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<XgStats>> GetXgStatsAsync(CancellationToken cancellationToken = default);
}
