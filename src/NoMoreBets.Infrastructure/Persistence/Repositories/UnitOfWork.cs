using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class UnitOfWork : IUnitOfWork
{
  private readonly AppDbContext _db;

  public UnitOfWork(
    AppDbContext db,
    ILeagueRepository leagueRepository,
    IMatchRepository matchRepository,
    IClubRepository clubRepository)
  {
    _db = db;
    Matches = matchRepository;
    Clubs = clubRepository;
    Leagues = leagueRepository;
  }

  public IMatchRepository Matches { get; }
  public IClubRepository Clubs { get; }

  public ILeagueRepository Leagues { get; }

  public async Task SaveChangesAsync(CancellationToken cancellationToken)
  {
    await _db.SaveChangesAsync(cancellationToken);
  }
}
