using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Features.SoccerData;

/// <summary>
/// HTTP client for SoccerData API with cache. Retries and per-attempt timeout are applied by the registered HttpClient resilience pipeline (e.g. Polly).
/// </summary>
public class SoccerDataClient : ISoccerDataClient
{
    private const string BaseUrl = "https://api.soccerdataapi.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        Converters = { new NullableDoubleConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly SoccerDataOptions _options;
    private readonly IJsonCache _cache;
    private readonly ILogger<SoccerDataClient> _logger;

    public SoccerDataClient(
        HttpClient httpClient,
        IOptions<SoccerDataOptions> options,
        IJsonCache cache,
        ILogger<SoccerDataClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeagueMatchPreviews>> GetMatchPreviewsUpcomingAsync(int? leagueId = null, CancellationToken cancellationToken = default)
    {
        var endpoint = "/match-previews-upcoming/";
        var cacheKey = BuildCacheKey(endpoint, null);
        var element = await LoadFromCacheOrFetchAsync(cacheKey, endpoint, null, cancellationToken).ConfigureAwait(false);
        if (element is null)
            return [];

        if (!element.Value.TryGetProperty("results", out var resultsProp) || resultsProp.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("SoccerData match-previews-upcoming response missing or invalid 'results' array");
            return [];
        }

        var list = new List<LeagueMatchPreviews>();
        foreach (var item in resultsProp.EnumerateArray())
        {
            try
            {
                var league = item.Deserialize<LeagueMatchPreviews>(JsonOptions);
                if (league is null) continue;
                if (leagueId is null || league.LeagueId == leagueId.Value)
                    list.Add(league);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse league item in match-previews-upcoming");
            }
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<MatchPreview> GetMatchPreviewAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var endpoint = "/match-preview/";
        var queryParams = new Dictionary<string, object?> { ["match_id"] = matchId };
        var cacheKey = BuildCacheKey(endpoint, queryParams);
        var element = await LoadFromCacheOrFetchAsync(cacheKey, endpoint, queryParams, cancellationToken).ConfigureAwait(false);
        if (element is null)
            throw new SoccerDataException($"No response for match preview {matchId}");

        var preview = element.Value.Deserialize<MatchPreview>(JsonOptions);
        if (preview is null)
            throw new SoccerDataException($"Failed to deserialize match preview {matchId}");
        return preview;
    }

    /// <inheritdoc />
    public async Task<HeadToHead> GetHeadToHeadAsync(int team1Id, int team2Id, CancellationToken cancellationToken = default)
    {
        var endpoint = "/head-to-head/";
        var queryParams = new Dictionary<string, object?>
        {
            ["team_1_id"] = team1Id,
            ["team_2_id"] = team2Id
        };
        var cacheKey = BuildCacheKey(endpoint, queryParams);
        var element = await LoadFromCacheOrFetchAsync(cacheKey, endpoint, queryParams, cancellationToken).ConfigureAwait(false);
        if (element is null)
            throw new SoccerDataException($"No response for head-to-head {team1Id} vs {team2Id}");

        var headToHead = element.Value.Deserialize<HeadToHead>(JsonOptions);
        if (headToHead is null)
            throw new SoccerDataException($"Failed to deserialize head-to-head {team1Id} vs {team2Id}");
        return headToHead;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LeagueMatches>> GetMatchesAsync(string? date = null, int? leagueId = null, string? season = null, CancellationToken cancellationToken = default)
    {
        var endpoint = "/matches/";
        var queryParams = new Dictionary<string, object?>();
        if (date is not null) queryParams["date"] = date;
        if (leagueId is not null) queryParams["league_id"] = leagueId;
        if (season is not null) queryParams["season"] = season;
        var cacheKey = BuildCacheKey(endpoint, queryParams.Count > 0 ? queryParams : null);
        var element = await LoadFromCacheOrFetchAsync(cacheKey, endpoint, queryParams.Count > 0 ? queryParams : null, cancellationToken).ConfigureAwait(false);
        if (element is null)
            return [];

        if (element.Value.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("SoccerData matches response is not an array");
            return [];
        }

        var list = new List<LeagueMatches>();
        foreach (var item in element.Value.EnumerateArray())
        {
            try
            {
                var league = item.Deserialize<LeagueMatches>(JsonOptions);
                if (league is not null)
                    list.Add(league);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse league item in matches");
            }
        }

        return list;
    }

    /// <summary>Build cache key from endpoint and params (excludes auth_token).</summary>
    internal static string BuildCacheKey(string endpoint, IReadOnlyDictionary<string, object?>? queryParams)
    {
        var normalized = endpoint.Trim('/').Replace("/", "_", StringComparison.Ordinal);
        if (queryParams is null || queryParams.Count == 0)
            return normalized + "_";

        var filtered = queryParams
            .Where(kv => kv.Key != "auth_token" && kv.Value is not null)
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}_{kv.Value}");
        return normalized + "_" + string.Join("_", filtered);
    }

    private async Task<JsonElement?> LoadFromCacheOrFetchAsync(
        string cacheKey,
        string endpoint,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.LoadAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached.HasValue)
            return cached;

        var requestParams = new Dictionary<string, string?>();
        if (queryParams is not null)
        {
            foreach (var (k, v) in queryParams)
                if (k != "auth_token" && v is not null)
                    requestParams[k] = v.ToString();
        }
        requestParams["auth_token"] = _options.ApiKey;

        var path = endpoint.TrimStart('/');
        var pathAndQuery = path + QueryHelpers.AddQueryString("", requestParams!);
        var requestUri = new Uri(new Uri(BaseUrl + "/"), pathAndQuery);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                throw new SoccerDataAuthException($"Authentication failed ({(int)response.StatusCode}). Check your API key.");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new SoccerDataNotFoundException($"Endpoint not found (404): {endpoint}");

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var element = doc.RootElement.Clone();
            await _cache.SaveAsync(cacheKey, element, cancellationToken).ConfigureAwait(false);
            return element;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SoccerDataAuthException)
        {
            throw;
        }
        catch (SoccerDataNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SoccerDataException($"Failed to fetch {requestUri.AbsoluteUri}", ex);
        }
    }
}
