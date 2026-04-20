using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Leagues;

public interface ILeagueProvider
{
  public Task<IReadOnlyList<TableEntry>> GetLeagueTableAsync(string leagueSlug, CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<XgStats>> GetXgStatsAsync(string leagueSlug, CancellationToken cancellationToken = default);
}
