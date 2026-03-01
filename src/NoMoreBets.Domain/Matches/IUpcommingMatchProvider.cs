using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoMoreBets.Domain.Matches;
public interface IUpcommingMatchProvider
{
  public async Task<IReadOnlyList<LeagueMatchPreviews>> GetMatchPreviewsUpcomingAsync(int? soccerdataLeagueId = null);
}
