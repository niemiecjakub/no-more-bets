using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Enums;
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

  public Task<Match?> GetMatchByIdAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return _db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);
  }

  public Task<Lineup?> GetLineup(int matchId)
  {
    return _db.Lineup.SingleOrDefaultAsync(l => l.MatchId == matchId);
  }

  public async Task<IReadOnlyList<Match>> GetRecentMatchesForClubAsync(int clubId, int count, DateOnly? upToDate = null, CancellationToken cancellationToken = default)
  {
    var query = _db.Match
      .Where(m => (m.HomeClubId == clubId || m.AwayClubId == clubId) && m.MatchStatusId == (int)MatchStatus.Finished);

    if (upToDate.HasValue)
      query = query.Where(m => DateOnly.FromDateTime(m.MatchDate) <= upToDate.Value);

    return await query
      .OrderByDescending(m => m.MatchDate)
      .Take(count)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
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

  public async Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .OrderBy(m => m.MatchDate)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Match>> GetUpcomingMatchesReadyForPredictionAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => _db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => _db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => _db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => _db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .OrderBy(m => m.MatchDate)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Match>> GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => _db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => _db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => _db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => _db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Where(m => !_db.MatchAnalysis.Any(a => a.MatchId == m.Id && a.Code == MatchAnalysis.ResearchCode))
      .OrderBy(m => m.MatchDate)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
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

  public async Task AddMatchAnalysisAsync(MatchAnalysis analysis, CancellationToken cancellationToken = default)
  {
    await _db.MatchAnalysis.AddAsync(analysis, cancellationToken);
  }

  public Task<MatchAnalysis?> GetLatestMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return _db.MatchAnalysis
      .Where(a => a.MatchId == matchId)
      .OrderByDescending(a => a.Id)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public Task<MatchAnalysis?> GetLatestMatchAnalysisByCodeAsync(int matchId, string code, CancellationToken cancellationToken = default)
  {
    return _db.MatchAnalysis
      .Where(a => a.MatchId == matchId && a.Code == code)
      .OrderByDescending(a => a.Id)
      .FirstOrDefaultAsync(cancellationToken);
  }
}
