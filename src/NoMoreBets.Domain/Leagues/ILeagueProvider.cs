using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Domain.Leagues;
public interface ILeagueProvider
{
  public async Task<IReadOnlyList<Club>> GetLeagueTableAsync(TableFilter filter);
  public async Task<IReadOnlyList<XgStats>> GetXgStatsAsync();
}
