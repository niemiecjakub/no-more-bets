using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Feedback;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Application.Common;
public interface IUnitOfWork
{
  IBettingRepository Betting { get; }
  IMatchRepository Matches { get; }
  IClubRepository Clubs { get; }
  ILeagueRepository Leagues { get; }
  IMemoryRepository Memories { get; }
  IBankrollRepository Bankroll { get; }
  IAgentSessionRepository AgentSessions { get; }
  IFeedbackRepository Feedback { get; }
  Task SaveChangesAsync(CancellationToken cancellationToken);
}
