using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class MatchRepository : IMatchRepository
{
  private readonly AppDbContext _db;
  public MatchRepository(AppDbContext db)
  {
    _db = db;
  }

  public Task<Head2Head?> GetHeadToHead(int team1, int team2)
  {
    return _db.Head2Head
      .ForClubs(team1, team2)
      .FirstOrDefaultAsync();
  }

  public Task<Lineup?> GetLineup(int matchId)
  {
    return _db.Lineup.SingleOrDefaultAsync(l => l.MatchId == matchId);
  }

  public Task<Match?> GetMatchBySoccerdataId(int soccerdataId)
  {
    return _db.Match.FirstOrDefaultAsync(m => m.SoccerdataId == soccerdataId);
  }

  public Task<List<Match>> GetMatches(DateTime date)
  {
    var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc).Date;
    return _db.Match
         .Where(m => m.MatchDate.Date == dateUtc)
         .Include(m => m.HomeClub)
         .Include(m => m.AwayClub)
         .ToListAsync();
  }

  public Task<MatchPreview?> GetMatchPreview(int matchId)
  {
    return _db.MatchPreview.FirstOrDefaultAsync(e => e.MatchId == matchId);
  }

  public async Task AddLineup(Lineup lineup)
  {
    await _db.Lineup.AddAsync(lineup);
  }

  public async Task AddMatch(Match match)
  {
    await _db.Match.AddAsync(match);
  }
  public async Task AddMatchPreview(MatchPreview matchPreview)
  {
    await _db.MatchPreview.AddAsync(matchPreview);
  }
}
