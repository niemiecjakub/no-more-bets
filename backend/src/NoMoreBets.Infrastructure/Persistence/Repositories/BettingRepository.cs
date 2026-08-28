using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public class BettingRepository : IBettingRepository
{
  private readonly AppDbContext _db;

  public BettingRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _db.BettingOddsSnapshot
      .Where(s => s.MatchId == matchId)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventTypeEntity)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventOptionEntity)
      .OrderByDescending(s => s.SnapshotTime)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<decimal?> GetCurrentOddsForSelectionAsync(int matchId, BettingEventType eventType, BettingEventOption eventOption, CancellationToken cancellationToken = default)
  {
    var snapshots = await GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (snapshots.Count == 0)
      return null;

    var latest = snapshots[0];
    var eventTypeId = (int)eventType;
    var optionId = (int)eventOption;
    foreach (var row in latest.Rows.Where(r => r.EventTypeId == eventTypeId && r.EventOptionId == optionId))
    {
      if (row.Odds.HasValue)
        return row.Odds.Value;
    }

    return null;
  }

  public async Task<IReadOnlyList<Match>> GetMatchesAvailableForBettingAsync(CancellationToken cancellationToken = default)
  {
    var matchIdsWithSnapshots = _db.BettingOddsSnapshot.Select(s => s.MatchId).Distinct();
    var matchIdsWithAnalysis = _db.MatchAnalysis.Select(a => a.MatchId).Distinct();

    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming
        && matchIdsWithSnapshots.Contains(m.Id)
        && matchIdsWithAnalysis.Contains(m.Id))
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<bool> AnyDailyPickOnDateAsync(DateOnly slipDate, CancellationToken cancellationToken = default) =>
    _db.DailyPick.AsNoTracking().AnyAsync(p => p.SlipDate == slipDate, cancellationToken);

  public Task<bool> AnyDailyPickOnDateWithRiskAsync(
    DateOnly slipDate,
    int riskLevelId,
    CancellationToken cancellationToken = default) =>
    _db.DailyPick.AsNoTracking().AnyAsync(
      p => p.SlipDate == slipDate && p.RiskLevelId == riskLevelId,
      cancellationToken);

  public async Task AddBetSlipAsync(BetSlip slip, CancellationToken cancellationToken = default)
  {
    await _db.BetSlip.AddAsync(slip, cancellationToken).ConfigureAwait(false);
  }

  public async Task<BetSlip?> GetBetSlipWithSelectionsByIdAsync(int betSlipId, CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .Include(s => s.Selections)
      .FirstOrDefaultAsync(s => s.Id == betSlipId, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default)
  {
    var query = _db.BetSlip.Where(s =>
      s.AgentSession == null
      || (s.AgentSession.Phase != AgentSessionPhase.Research
        && s.AgentSession.Phase != AgentSessionPhase.DailySlip));
    if (slipStatus is { } status)
    {
      query = query.Where(s => s.StatusId == (int)status);
    }

    return await MaterializeBetSlipListAsync(query, cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBettingPhaseBetSlipsAsync(
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var query = ApplySeasonYearFilter(
      _db.BetSlip
        .AsNoTracking()
        .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting),
      seasonYears);

    return await query
      .Include(s => s.BetStatusEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBetSlipsByAgentSessionIdAsync(
    int agentSessionId,
    CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSessionId == agentSessionId)
      .Include(s => s.BetStatusEntity)
      .Include(s => s.DailyPick)
        .ThenInclude(p => p!.RiskLevel)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<BetSlip?> GetLatestResearchBetSlipForMatchAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .AsSplitQuery()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(s => s.Selections.Any(sel => sel.MatchId == matchId))
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<BetSlip>> MaterializeBetSlipListAsync(
    IQueryable<BetSlip> query,
    CancellationToken cancellationToken)
  {
    return await query
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsCreatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default)
  {
    var sinceUtc = DateTime.UtcNow.AddDays(-lastDays);
    return await _db.BetSlip
      .Where(s => s.StatusId != (int)BetStatus.Pending && s.CreatedAt >= sinceUtc)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default)
  {
    var sinceUtc = DateTime.UtcNow.AddDays(-lastDays);
    return await _db.BetSlip
      .Where(s => s.StatusId != (int)BetStatus.Pending && s.UpdatedAt != null && s.UpdatedAt >= sinceUtc)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.UpdatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .Where(s =>
        s.StatusId != (int)BetStatus.Pending
        && s.AgentSessionReflectedId == null
        && (s.AgentSession == null
          || (s.AgentSession.Phase != AgentSessionPhase.Research
            && s.AgentSession.Phase != AgentSessionPhase.DailySlip)))
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task MarkBetSlipsAgentSessionReflectedAsync(
    int agentSessionId,
    IReadOnlyList<int> betSlipIds,
    CancellationToken cancellationToken = default)
  {
    if (betSlipIds.Count == 0)
    {
      return;
    }

    var distinctIds = betSlipIds.Distinct().ToList();
    var utcNow = DateTime.UtcNow;
    await _db.BetSlip
      .Where(s => distinctIds.Contains(s.Id))
      .ExecuteUpdateAsync(
        s => s
          .SetProperty(b => b.AgentSessionReflectedId, agentSessionId)
          .SetProperty(b => b.UpdatedAt, utcNow),
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSelection>> GetPendingSelectionsWithBothScoresAsync(
    CancellationToken cancellationToken = default)
  {
    return await _db.BetSelection
      .Where(s => s.StatusId == (int)BetStatus.Pending)
      .Where(s => s.Match.HomeGoals != null && s.Match.AwayGoals != null)
      .Include(s => s.Match)
      .Include(s => s.BetSlip)
        .ThenInclude(b => b.AgentSession)
      .Include(s => s.BetSlip)
      .ThenInclude(b => b.Selections)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlySet<int>> GetMatchIdsWithResearchPhaseSelectionsAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default)
  {
    if (matchIds.Count == 0)
      return new HashSet<int>();

    var ids = await _db.BetSelection
      .Where(sel =>
        matchIds.Contains(sel.MatchId)
        && sel.BetSlip.AgentSession != null
        && sel.BetSlip.AgentSession.Phase == AgentSessionPhase.Research)
      .Select(sel => sel.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return ids.ToHashSet();
  }

  private IQueryable<BetSlip> SettledBettingSlipsQuery() =>
    _db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting)
      .Where(s => s.StatusId != (int)BetStatus.Pending);

  private static string[] NormalizeSeasonYears(IReadOnlyList<string>? seasonYears) =>
    (seasonYears ?? [])
      .Where(y => !string.IsNullOrWhiteSpace(y))
      .Select(y => y.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();

  private static IQueryable<BetSlip> ApplySeasonYearFilter(
    IQueryable<BetSlip> query,
    IReadOnlyList<string>? seasonYears)
  {
    var selectedSeasonYears = NormalizeSeasonYears(seasonYears);
    if (selectedSeasonYears.Length == 0)
      return query;

    return query.Where(s => s.Selections.Any(sel =>
      sel.Match != null &&
      sel.Match.Stage != null &&
      sel.Match.Stage.Season != null &&
      selectedSeasonYears.Contains(sel.Match.Stage.Season.Year)));
  }

  public async Task<BettingPhaseSummaryStats> GetBettingPhaseSettledSummaryAsync(
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var settled = await ApplySeasonYearFilter(SettledBettingSlipsQuery(), seasonYears)
      .Select(s => new { s.StatusId, SelectionsCount = s.Selections.Count })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var settledCount = settled.Count;
    var wonCount = settled.Count(s => s.StatusId == (int)BetStatus.Won);
    var lostCount = settled.Count(s => s.StatusId == (int)BetStatus.Lost);

    return new BettingPhaseSummaryStats(
      settledCount,
      settled.Sum(s => s.SelectionsCount),
      wonCount,
      lostCount);
  }

  public async Task<ResearchPhaseSummaryStats> GetResearchPhaseSettledSummaryAsync(
    IReadOnlyList<int> leagueIds,
    IReadOnlyList<string> seasonYears,
    CancellationToken cancellationToken = default)
  {
    var selectedSeasonYears = seasonYears
      .Where(y => !string.IsNullOrWhiteSpace(y))
      .Select(y => y.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();
    var hasLeagueFilter = leagueIds.Count > 0;
    var hasSeasonYearFilter = selectedSeasonYears.Length > 0;

    var settledQuery = _db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(s => s.StatusId != (int)BetStatus.Pending);

    if (hasLeagueFilter || hasSeasonYearFilter)
    {
      settledQuery = settledQuery
        .Where(s => s.Selections.Any(sel =>
          sel.Match != null &&
          sel.Match.Stage != null &&
          sel.Match.Stage.Season != null &&
          (!hasLeagueFilter || leagueIds.Contains(sel.Match.Stage.Season.LeagueId)) &&
          (!hasSeasonYearFilter || selectedSeasonYears.Contains(sel.Match.Stage.Season.Year))));
    }

    var settledSelections = await settledQuery
      .SelectMany(s => s.Selections)
      .Where(sel =>
        (!hasLeagueFilter || (
          sel.Match != null &&
          sel.Match.Stage != null &&
          sel.Match.Stage.Season != null &&
          leagueIds.Contains(sel.Match.Stage.Season.LeagueId))) &&
        (!hasSeasonYearFilter || (
          sel.Match != null &&
          sel.Match.Stage != null &&
          sel.Match.Stage.Season != null &&
          selectedSeasonYears.Contains(sel.Match.Stage.Season.Year))))
      .Select(sel => sel.StatusId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var won = settledSelections.Count(statusId => statusId == (int)BetStatus.Won);
    var lost = settledSelections.Count(statusId => statusId == (int)BetStatus.Lost);

    return new ResearchPhaseSummaryStats(settledSelections.Count, won, lost);
  }

  public async Task<IReadOnlyList<ResearchPhaseScenarioLegRow>> GetResearchPhaseSettledScenarioLegsAsync(
    IReadOnlyList<int> leagueIds,
    IReadOnlyList<string> seasonYears,
    CancellationToken cancellationToken = default)
  {
    var selectedSeasonYears = seasonYears
      .Where(y => !string.IsNullOrWhiteSpace(y))
      .Select(y => y.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();
    var hasLeagueFilter = leagueIds.Count > 0;
    var hasSeasonYearFilter = selectedSeasonYears.Length > 0;

    var settledQuery = _db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(s => s.StatusId != (int)BetStatus.Pending);

    if (hasLeagueFilter || hasSeasonYearFilter)
    {
      settledQuery = settledQuery
        .Where(s => s.Selections.Any(sel =>
          sel.Match != null &&
          sel.Match.Stage != null &&
          sel.Match.Stage.Season != null &&
          (!hasLeagueFilter || leagueIds.Contains(sel.Match.Stage.Season.LeagueId)) &&
          (!hasSeasonYearFilter || selectedSeasonYears.Contains(sel.Match.Stage.Season.Year))));
    }

    var rows = await settledQuery
      .SelectMany(s => s.Selections.Select(sel => new
      {
        SlipId = s.Id,
        sel.OddsAtPlacement,
        sel.StatusId,
      }))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows
      .Select(r => new ResearchPhaseScenarioLegRow(r.SlipId, r.OddsAtPlacement, (BetStatus)r.StatusId))
      .ToList();
  }

  public async Task<BettingPhaseDetailCounts> GetBettingPhaseSettledDetailCountsAsync(
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var settledSlips = ApplySeasonYearFilter(SettledBettingSlipsQuery(), seasonYears);
    var wonSlips = await settledSlips.CountAsync(s => s.StatusId == (int)BetStatus.Won, cancellationToken).ConfigureAwait(false);
    var lostSlips = await settledSlips.CountAsync(s => s.StatusId == (int)BetStatus.Lost, cancellationToken).ConfigureAwait(false);

    var selectedSeasonYears = NormalizeSeasonYears(seasonYears);
    var settledSelections = _db.BetSelection
      .AsNoTracking()
      .Where(sel => sel.BetSlip.AgentSession != null && sel.BetSlip.AgentSession.Phase == AgentSessionPhase.Betting)
      .Where(sel => sel.BetSlip.StatusId == (int)BetStatus.Won || sel.BetSlip.StatusId == (int)BetStatus.Lost);

    if (selectedSeasonYears.Length > 0)
    {
      settledSelections = settledSelections.Where(sel =>
        sel.BetSlip.Selections.Any(slipSel =>
          slipSel.Match != null &&
          slipSel.Match.Stage != null &&
          slipSel.Match.Stage.Season != null &&
          selectedSeasonYears.Contains(slipSel.Match.Stage.Season.Year)));
    }

    var wonSelections = await settledSelections.CountAsync(sel => sel.StatusId == (int)BetStatus.Won, cancellationToken).ConfigureAwait(false);
    var lostSelections = await settledSelections.CountAsync(sel => sel.StatusId == (int)BetStatus.Lost, cancellationToken).ConfigureAwait(false);

    return new BettingPhaseDetailCounts(wonSlips, lostSlips, wonSelections, lostSelections);
  }

  public async Task<BetSlipIdPage> GetSettledBettingSlipIdsPageAsync(
    int limit,
    DateTime? afterCreatedAtUtc,
    int? afterId,
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var query = ApplySeasonYearFilter(SettledBettingSlipsQuery(), seasonYears)
      .Where(s => s.StatusId == (int)BetStatus.Won || s.StatusId == (int)BetStatus.Lost);

    if (afterCreatedAtUtc is not null && afterId is not null)
    {
      var cursorCreatedAt = afterCreatedAtUtc.Value;
      var cursorId = afterId.Value;
      query = query.Where(s =>
        s.CreatedAt < cursorCreatedAt
        || (s.CreatedAt == cursorCreatedAt && s.Id < cursorId));
    }

    var slipIds = await query
      .OrderByDescending(s => s.CreatedAt)
      .ThenByDescending(s => s.Id)
      .Take(limit + 1)
      .Select(s => s.Id)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var hasMore = slipIds.Count > limit;
    if (hasMore)
      slipIds.RemoveAt(slipIds.Count - 1);

    return new BetSlipIdPage(slipIds, hasMore);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBettingPhaseBetSlipsByIdsAsync(
    IReadOnlyList<int> slipIds,
    CancellationToken cancellationToken = default)
  {
    if (slipIds.Count == 0)
      return Array.Empty<BetSlip>();

    var slips = await _db.BetSlip
      .AsNoTracking()
      .Where(s => slipIds.Contains(s.Id))
      .Include(s => s.BetStatusEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var order = slipIds.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
    slips.Sort((left, right) => order[left.Id].CompareTo(order[right.Id]));
    return slips;
  }

  public async Task<PendingBetsWidgetData> GetBettingPhasePendingBetsWidgetAsync(
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    var pending = await ApplySeasonYearFilter(
        _db.BetSlip
          .AsNoTracking()
          .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting)
          .Where(s => s.StatusId == (int)BetStatus.Pending),
        seasonYears)
      .Select(s => new { s.StakeAmount, s.PotentialPayout, s.CreatedAt })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return new PendingBetsWidgetData(
      pending.Count,
      pending.Sum(s => s.StakeAmount),
      pending.Sum(s => s.PotentialPayout),
      pending.OrderByDescending(s => s.CreatedAt).Select(s => (DateTime?)s.CreatedAt).FirstOrDefault());
  }

  public async Task<ClubBetSelectionStats> GetResearchPhaseSettledSelectionStatsForClubAsync(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var settledSelections = _db.BetSelection
      .AsNoTracking()
      .Where(sel => sel.BetSlip.AgentSession != null && sel.BetSlip.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(sel => sel.StatusId == (int)BetStatus.Won || sel.StatusId == (int)BetStatus.Lost)
      .Where(sel => sel.Match.HomeClubId == clubId || sel.Match.AwayClubId == clubId);

    var wonCount = await settledSelections
      .CountAsync(sel => sel.StatusId == (int)BetStatus.Won, cancellationToken)
      .ConfigureAwait(false);
    var lostCount = await settledSelections
      .CountAsync(sel => sel.StatusId == (int)BetStatus.Lost, cancellationToken)
      .ConfigureAwait(false);

    return new ClubBetSelectionStats(wonCount, lostCount, wonCount + lostCount);
  }
}
