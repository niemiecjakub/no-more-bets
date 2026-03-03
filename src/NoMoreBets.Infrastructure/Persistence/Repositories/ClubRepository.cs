using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class ClubRepository : IClubRepository
{
  private readonly AppDbContext _db;
  public ClubRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task AddHead2Head(Head2Head head2Head)
  {
    await _db.Head2Head.AddAsync(head2Head);
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
