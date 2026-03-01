using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class ClubRepository : IClubRepository
{
  private readonly AppDbContext _db;
  public ClubRepository(AppDbContext db)
  {
    _db = db;
  }

  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds)
  {
    return _db.Club
      .Where(c => soccerdataIds.Contains(c.SoccerdataId))
      .ToListAsync();
  }

  public Task<List<Club>> GetClubs(int leagueId)
  {
    return _db.Club
      .Where(c => c.LeagueId == leagueId)
      .ToListAsync();
  }
}
