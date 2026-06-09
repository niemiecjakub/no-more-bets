using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Feedback;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class UnitOfWork : IUnitOfWork
{
  private readonly AppDbContext _db;

  public UnitOfWork(
    AppDbContext db,
    IBettingRepository bettingRepository,
    ILeagueRepository leagueRepository,
    IMatchRepository matchRepository,
    IClubRepository clubRepository,
    IMemoryRepository memoryRepository,
    IBankrollRepository bankrollRepository,
    IAgentSessionRepository agentSessionRepository,
    IFeedbackRepository feedbackRepository)
  {
    _db = db;
    Betting = bettingRepository;
    Matches = matchRepository;
    Clubs = clubRepository;
    Leagues = leagueRepository;
    Memories = memoryRepository;
    Bankroll = bankrollRepository;
    AgentSessions = agentSessionRepository;
    Feedback = feedbackRepository;
  }

  public IBettingRepository Betting { get; }
  public IMatchRepository Matches { get; }
  public IClubRepository Clubs { get; }

  public ILeagueRepository Leagues { get; }
  public IMemoryRepository Memories { get; }
  public IBankrollRepository Bankroll { get; }
  public IAgentSessionRepository AgentSessions { get; }
  public IFeedbackRepository Feedback { get; }

  public async Task SaveChangesAsync(CancellationToken cancellationToken)
  {
    return;
    //await _db.SaveChangesAsync(cancellationToken);
  }
}
