using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Matches;

public interface IMatchRepository
{
  public Task<Match?> GetMatchByIdAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<Match>> GetMatchesByIdsAsync(
    IReadOnlyList<int> matchIds,
    CancellationToken cancellationToken = default);
  public Task<Match?> GetMatchBySoccerdataId(int soccerdataId);
  public Task<MatchDetails?> GetMatchDetailsByFotmobUrlAsync(string fotmobUrl, CancellationToken cancellationToken = default);
  public Task<MatchDetails?> GetMatchDetailsByMatchIdAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<MatchPreview?> GetMatchPreview(int matchId);
  public Task<Head2Head?> GetHeadToHead(int team1, int team2);
  public Task<List<Match>> GetMatches(DateTime date);
  public Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<Match>> GetUpcomingMatchesWithOddsSnapshotsAsync(CancellationToken cancellationToken = default);
  public Task<Lineup?> GetLineup(int matchId);
  public Task<IReadOnlyList<MatchEvent>> GetMatchEventsForMatchAsync(
    int matchId,
    CancellationToken cancellationToken = default);
  /// <param name="upToDate">When set, only finished matches strictly before this calendar day (exclusive upper bound).</param>
  public Task<IReadOnlyList<Match>> GetRecentMatchesForClubAsync(int clubId, int count, DateOnly? upToDate = null, CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<Match>> GetMatchesForClubAsync(int clubId, CancellationToken cancellationToken = default);
  public Task<Match?> GetNextUpcomingMatchForClubAsync(int clubId, CancellationToken cancellationToken = default);
  public Task<IReadOnlyDictionary<int, IReadOnlyList<MatchResult>>> GetFormForClubsInSeasonAsync(
    int seasonId,
    IReadOnlyList<int> clubIds,
    int count = 5,
    CancellationToken cancellationToken = default);
  public Task AddMatch(Match match, CancellationToken cancellationToken = default);
  public Task AddMatchDetailsAsync(MatchDetails matchDetails, CancellationToken cancellationToken = default);
  public Task AddLineup(Lineup lineup);
  public Task AddMatchPreview(MatchPreview matchPreview);
  public Task AddMatchAnalysisAsync(MatchAnalysis analysis, CancellationToken cancellationToken = default);
  public Task<MatchAnalysis?> GetLatestMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<MatchAnalysis?> GetLatestMatchAnalysisByCodeAsync(int matchId, string code, CancellationToken cancellationToken = default);
  public Task<IReadOnlySet<int>> GetMatchIdsWithAnalysisCodeAsync(
    IReadOnlyCollection<int> matchIds,
    string code,
    CancellationToken cancellationToken = default);
  Task<MatchPage> GetMatchesPageAsync(
    int limit,
    int? matchStatusId,
    IReadOnlyList<int> leagueIds,
    DateTime? afterMatchDateUtc,
    int? afterId,
    MatchDateSortOrder sortOrder = MatchDateSortOrder.Descending,
    string? search = null,
    IReadOnlyList<string>? seasonYears = null,
    CancellationToken cancellationToken = default);
  Task<IReadOnlySet<int>> GetMatchIdsWithLineupAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default);
  Task<IReadOnlyDictionary<int, MatchResultOdds>> GetLatestMatchResultOddsAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default);
  Task<IReadOnlySet<int>> GetMatchIdsWithHeadToHeadAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default);
  Task<IReadOnlyList<MatchAnalysis>> GetNonResearchAnalysesForMatchAsync(
    int matchId,
    CancellationToken cancellationToken = default);
}
