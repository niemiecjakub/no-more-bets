using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class MatchRepository : IMatchRepository
{
  private readonly AppDbContext _db;
  private readonly ILogger<MatchRepository> _logger;

  public MatchRepository(AppDbContext db, ILogger<MatchRepository> logger)
  {
    _db = db;
    _logger = logger;
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

  public Task<MatchDetails?> GetMatchDetailsByFotmobUrlAsync(string fotmobUrl, CancellationToken cancellationToken = default)
  {
    return _db.MatchDetails
      .Include(md => md.Match)
      .FirstOrDefaultAsync(md => md.FotmobUrl == fotmobUrl, cancellationToken);
  }

  public Task<MatchDetails?> GetMatchDetailsByMatchIdAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return _db.MatchDetails
      .Include(md => md.Match)
      .FirstOrDefaultAsync(md => md.MatchId == matchId, cancellationToken);
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

  public async Task AddMatch(Match match, CancellationToken cancellationToken = default)
  {
    if (match.SoccerdataId.HasValue)
    {
      var existsBySoccerdata = await _db.Match.AnyAsync(m => m.SoccerdataId == match.SoccerdataId.Value, cancellationToken);
      if (existsBySoccerdata)
      {
        _logger.LogWarning("A match with SoccerdataId {SoccerdataId} already exists.", match.SoccerdataId.Value);
        return;
      }
    }

    if (!string.IsNullOrWhiteSpace(match.BetclicUrl))
    {
      var existsByBetclic = await _db.Match.AnyAsync(m => m.BetclicUrl != null && m.BetclicUrl == match.BetclicUrl, cancellationToken);
      if (existsByBetclic)
      {
        _logger.LogWarning("A match with BetclicUrl '{BetclicUrl}' already exists.", match.BetclicUrl);
        return;
      }
    }

    await _db.Match.AddAsync(match, cancellationToken);
  }

  public async Task AddMatchDetailsAsync(MatchDetails matchDetails, CancellationToken cancellationToken = default)
  {
    await _db.MatchDetails.AddAsync(matchDetails, cancellationToken);
  }
  
  public async Task AddMatchPreview(MatchPreview matchPreview)
  {
    await _db.MatchPreview.AddAsync(matchPreview);
  }
}
