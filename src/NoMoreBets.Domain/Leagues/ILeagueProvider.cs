using NoMoreBets.Domain.Leagues.Dto;

namespace NoMoreBets.Domain.Leagues;
public interface ILeagueProvider
{
  public Task<IReadOnlyList<TableEntry>> GetLeagueTableAsync(CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<XgStats>> GetXgStatsAsync(CancellationToken cancellationToken = default);
}
