namespace NoMoreBets.Domain.Matches;

public interface IMatchRepository
{
  public Task<Match?> GetMatchByIdAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<Match?> GetMatchBySoccerdataId(int soccerdataId);
  public Task<MatchDetails?> GetMatchDetailsByFotmobUrlAsync(string fotmobUrl, CancellationToken cancellationToken = default);
  public Task<MatchDetails?> GetMatchDetailsByMatchIdAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<MatchPreview?> GetMatchPreview(int matchId);
  public Task<Head2Head?> GetHeadToHead(int team1, int team2);
  public Task<List<Match>> GetMatches(DateTime date);
  public Task<IReadOnlyList<Match>> GetUpcomingMatchesAsync(CancellationToken cancellationToken = default);
  public Task<IReadOnlyList<Match>> GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(CancellationToken cancellationToken = default);
  public Task<Lineup?> GetLineup(int matchId);
  public Task<IReadOnlyList<Match>> GetRecentMatchesForClubAsync(int clubId, int count, DateOnly? upToDate = null, CancellationToken cancellationToken = default);
  public Task AddMatch(Match match, CancellationToken cancellationToken = default);
  public Task AddMatchDetailsAsync(MatchDetails matchDetails, CancellationToken cancellationToken = default);
  public Task AddLineup(Lineup lineup);
  public Task AddMatchPreview(MatchPreview matchPreview);
  public Task AddMatchAnalysisAsync(MatchAnalysis analysis, CancellationToken cancellationToken = default);
  public Task<MatchAnalysis?> GetLatestMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default);
  public Task<MatchAnalysis?> GetLatestMatchAnalysisByCodeAsync(int matchId, string code, CancellationToken cancellationToken = default);
}
