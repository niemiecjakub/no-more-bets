using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;
public interface IUnitOfWork
{
  IMatchRepository Matches { get; }
  IClubRepository Clubs { get; }
  ILeagueRepository Leagues { get; }
  Task SaveChangesAsync(CancellationToken cancellationToken);
}
