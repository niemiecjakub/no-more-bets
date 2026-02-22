using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData;

/// <summary>
/// Client for SoccerData API (api.soccerdataapi.com).
/// Uses cache and retries; auth via query param auth_token.
/// </summary>
public interface ISoccerDataClient
{
    /// <summary>Gets upcoming match previews, optionally filtered by league ID.</summary>
    Task<IReadOnlyList<LeagueMatchPreviews>> GetMatchPreviewsUpcomingAsync(int? leagueSoccerdataId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets match preview for a single match.</summary>
    Task<MatchPreview> GetMatchPreviewAsync(int matchISoccerdatad, CancellationToken cancellationToken = default);

    /// <summary>Gets head-to-head data between two teams.</summary>
    Task<HeadToHead> GetHeadToHeadAsync(int team1SoccerdataId, int team2SoccerdataId, CancellationToken cancellationToken = default);

    /// <summary>Gets matches by date, league, and/or season (combinations as per API).</summary>
    Task<IReadOnlyList<LeagueMatches>> GetMatchesAsync(string? date = null, int? leagueSoccerdataId = null, string? season = null, CancellationToken cancellationToken = default);
}
