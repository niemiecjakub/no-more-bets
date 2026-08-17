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

  public async Task<IReadOnlyList<Match>> GetMatchesByIdsAsync(
    IReadOnlyList<int> matchIds,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return [];

    var ids = matchIds.Distinct().ToList();
    return await _db.Match
      .AsNoTracking()
      .Where(m => ids.Contains(m.Id))
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<Lineup?> GetLineup(int matchId)
  {
    return _db.Lineup.SingleOrDefaultAsync(l => l.MatchId == matchId);
  }

  public async Task<IReadOnlyList<MatchEvent>> GetMatchEventsForMatchAsync(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    return await _db.MatchEvent
      .AsNoTracking()
      .Where(e => e.MatchId == matchId)
      .Include(e => e.Player)
      .Include(e => e.EventTypeEntity)
      .OrderBy(e => e.Minute)
      .ThenBy(e => e.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<Match?> GetNextUpcomingMatchForClubAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => m.HomeClubId == clubId || m.AwayClubId == clubId)
      .OrderBy(m => m.MatchDate)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Match>> GetRecentMatchesForClubAsync(int clubId, int count, DateOnly? upToDate = null, CancellationToken cancellationToken = default)
  {
    var query = _db.Match
      .Where(m => (m.HomeClubId == clubId || m.AwayClubId == clubId) && m.MatchStatusId == (int)MatchStatus.Finished);

    // Exclusive: prior form for a fixture dated D must not include matches on D (e.g. the same game when finished).
    if (upToDate.HasValue)
      query = query.Where(m => DateOnly.FromDateTime(m.MatchDate) < upToDate.Value);

    return await query
      .OrderByDescending(m => m.MatchDate)
      .Take(count)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Match>> GetMatchesForClubAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .AsNoTracking()
      .Where(m => m.HomeClubId == clubId || m.AwayClubId == clubId)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .OrderByDescending(m => m.MatchDate)
      .ThenByDescending(m => m.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyDictionary<int, IReadOnlyList<MatchResult>>> GetFormForClubsInSeasonAsync(
    int seasonId,
    IReadOnlyList<int> clubIds,
    int count = 5,
    CancellationToken cancellationToken = default)
  {
    var ids = clubIds.Distinct().ToList();
    if (ids.Count == 0)
      return new Dictionary<int, IReadOnlyList<MatchResult>>();

    var idSet = ids.ToHashSet();
    var matches = await _db.Match
      .AsNoTracking()
      .Where(m => m.MatchStatusId == (int)MatchStatus.Finished)
      .Where(m => m.StageId != null)
      .Where(m => m.Stage!.SeasonId == seasonId)
      .Where(m => idSet.Contains(m.HomeClubId) || idSet.Contains(m.AwayClubId))
      .OrderByDescending(m => m.MatchDate)
      .Select(m => new { m.HomeClubId, m.AwayClubId, m.HomeGoals, m.AwayGoals })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var formByClub = ids.ToDictionary(id => id, _ => new List<MatchResult>());

    foreach (var m in matches)
    {
      foreach (var clubId in new[] { m.HomeClubId, m.AwayClubId })
      {
        if (!idSet.Contains(clubId))
          continue;

        var list = formByClub[clubId];
        if (list.Count >= count)
          continue;

        list.Add(ToMatchResult(clubId, m.HomeClubId, m.AwayClubId, m.HomeGoals, m.AwayGoals));
      }

      if (formByClub.Values.All(l => l.Count >= count))
        break;
    }

    return formByClub.ToDictionary(
      kv => kv.Key,
      kv => (IReadOnlyList<MatchResult>)kv.Value.AsEnumerable().Reverse().ToList());
  }

  private static MatchResult ToMatchResult(int clubId, int homeClubId, int awayClubId, int? homeGoals, int? awayGoals)
  {
    var home = homeGoals ?? 0;
    var away = awayGoals ?? 0;
    if (clubId == homeClubId)
      return home > away ? MatchResult.Win : home < away ? MatchResult.Loss : MatchResult.Draw;

    return away > home ? MatchResult.Win : away < home ? MatchResult.Loss : MatchResult.Draw;
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

  public async Task<IReadOnlyList<Match>> GetUpcomingMatchesWithOddsSnapshotsAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => _db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .OrderBy(m => m.MatchDate)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<Match>> GetUpcomingMatchesWithAnalysisCodeAsync(
    string code,
    CancellationToken cancellationToken = default)
  {
    return await _db.Match
      .AsNoTracking()
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => _db.MatchAnalysis.Any(a => a.MatchId == m.Id && a.Code == code))
      .OrderBy(m => m.MatchDate)
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
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

  public async Task<IReadOnlySet<int>> GetMatchIdsWithAnalysisCodeAsync(
    IReadOnlyCollection<int> matchIds,
    string code,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return new HashSet<int>();

    var ids = await _db.MatchAnalysis
      .Where(a => a.Code == code && matchIds.Contains(a.MatchId))
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return ids.ToHashSet();
  }

  public async Task<MatchPage> GetMatchesPageAsync(
    int limit,
    int? matchStatusId,
    IReadOnlyList<int> leagueIds,
    DateTime? afterMatchDateUtc,
    int? afterId,
    MatchDateSortOrder sortOrder = MatchDateSortOrder.Descending,
    string? search = null,
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var selectedLeagueIds = leagueIds.Distinct().ToArray();
    var hasLeagueFilter = selectedLeagueIds.Length > 0;
    var selectedSeasonYears = (seasonYears ?? [])
      .Where(y => !string.IsNullOrWhiteSpace(y))
      .Select(y => y.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();
    var hasSeasonYearFilter = selectedSeasonYears.Length > 0;

    var matchesQuery = _db.Match.AsNoTracking().AsQueryable();
    if (matchStatusId.HasValue)
      matchesQuery = matchesQuery.Where(m => m.MatchStatusId == matchStatusId.Value);
    if (hasLeagueFilter)
      matchesQuery = matchesQuery.Where(m =>
        m.Stage != null &&
        selectedLeagueIds.Contains(m.Stage.Season.LeagueId));
    if (hasSeasonYearFilter)
      matchesQuery = matchesQuery.Where(m =>
        m.Stage != null &&
        selectedSeasonYears.Contains(m.Stage.Season.Year));

    if (!string.IsNullOrWhiteSpace(search))
    {
      var term = search.Trim().ToLowerInvariant();
      matchesQuery = matchesQuery.Where(m =>
        m.HomeClub.Name.ToLower().Contains(term)
        || m.AwayClub.Name.ToLower().Contains(term));
    }

    if (afterMatchDateUtc is not null && afterId is not null)
    {
      var cursorMatchDate = afterMatchDateUtc.Value;
      var cursorId = afterId.Value;
      matchesQuery = sortOrder == MatchDateSortOrder.Ascending
        ? matchesQuery.Where(m =>
          m.MatchDate > cursorMatchDate
          || (m.MatchDate == cursorMatchDate && m.Id > cursorId))
        : matchesQuery.Where(m =>
          m.MatchDate < cursorMatchDate
          || (m.MatchDate == cursorMatchDate && m.Id < cursorId));
    }

    var orderedQuery = sortOrder == MatchDateSortOrder.Ascending
      ? matchesQuery.OrderBy(m => m.MatchDate).ThenBy(m => m.Id)
      : matchesQuery.OrderByDescending(m => m.MatchDate).ThenByDescending(m => m.Id);

    var rows = await orderedQuery
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .Take(limit + 1)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    return new MatchPage(rows, hasMore);
  }

  public async Task<IReadOnlySet<int>> GetMatchIdsWithLineupAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return new HashSet<int>();

    var ids = await _db.Lineup
      .Where(l => matchIds.Contains(l.MatchId))
      .Select(l => l.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return ids.ToHashSet();
  }

  public async Task<IReadOnlyDictionary<int, MatchResultOdds>> GetLatestMatchResultOddsAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return new Dictionary<int, MatchResultOdds>();

    var snapshots = await _db.BettingOddsSnapshot
      .AsNoTracking()
      .Where(s => matchIds.Contains(s.MatchId))
      .Select(s => new { s.Id, s.MatchId, s.SnapshotTime })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    if (snapshots.Count == 0)
      return new Dictionary<int, MatchResultOdds>();

    var latestIdByMatch = snapshots
      .GroupBy(s => s.MatchId)
      .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(s => s.SnapshotTime).First().Id);

    var snapshotIds = latestIdByMatch.Values.ToList();
    var matchIdBySnapshotId = latestIdByMatch.ToDictionary(kv => kv.Value, kv => kv.Key);

    const int homeOptionId = (int)BettingEventOption.MatchResult_Home;
    const int awayOptionId = (int)BettingEventOption.MatchResult_Away;
    const int drawOptionId = (int)BettingEventOption.MatchResult_Draw;

    var rows = await _db.BettingOddsSnapshotRow
      .AsNoTracking()
      .Where(r =>
        snapshotIds.Contains(r.SnapshotId)
        && r.EventTypeId == (int)BettingEventType.MatchResult
        && r.EventOptionId != null
        && r.Odds != null
        && (r.EventOptionId == homeOptionId
          || r.EventOptionId == awayOptionId
          || r.EventOptionId == drawOptionId))
      .Select(r => new { r.SnapshotId, OptionId = r.EventOptionId!.Value, Odds = r.Odds!.Value })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var result = new Dictionary<int, MatchResultOdds>();
    foreach (var group in rows.GroupBy(r => r.SnapshotId))
    {
      if (!matchIdBySnapshotId.TryGetValue(group.Key, out var matchId))
        continue;

      decimal? home = null;
      decimal? draw = null;
      decimal? away = null;
      foreach (var row in group)
      {
        if (row.OptionId == homeOptionId)
          home = row.Odds;
        else if (row.OptionId == drawOptionId)
          draw = row.Odds;
        else if (row.OptionId == awayOptionId)
          away = row.Odds;
      }

      if (home is null && draw is null && away is null)
        continue;

      result[matchId] = new MatchResultOdds(home, draw, away);
    }

    return result;
  }

  public async Task<IReadOnlySet<int>> GetMatchIdsWithHeadToHeadAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return new HashSet<int>();

    var ids = await _db.Match
      .Where(m => matchIds.Contains(m.Id) && _db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Select(m => m.Id)
      .Distinct()
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return ids.ToHashSet();
  }

  public async Task<IReadOnlyList<MatchAnalysis>> GetNonResearchAnalysesForMatchAsync(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    return await _db.MatchAnalysis
      .Where(a => a.MatchId == matchId)
      .Where(a => a.Code != MatchAnalysis.ResearchCode && a.Code != MatchAnalysis.StructuredResearchCode)
      .OrderBy(a => a.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

}
