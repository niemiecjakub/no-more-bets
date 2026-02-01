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
/// HTTP client for SoccerData API with cache and optional retries (via Polly when registered).
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
        EnsureApiKey();
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
        EnsureApiKey();
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
        EnsureApiKey();
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
        EnsureApiKey();
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

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new SoccerDataAuthException("SoccerData API key is required. Set SoccerData:ApiKey in User Secrets or environment.");
    }

    /// <summary>Build cache key from endpoint and params (excludes auth_token). Matches Python format.</summary>
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

        Exception? lastException = null;
        for (var attempt = 1; attempt <= _options.RetryCount; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                request.Headers.Accept.ParseAdd("application/json");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                    throw new SoccerDataAuthException($"Authentication failed ({(int)response.StatusCode}). Check your API key.");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new SoccerDataNotFoundException($"Endpoint not found (404): {endpoint}");

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                var element = doc.RootElement.Clone();
                await _cache.SaveAsync(cacheKey, element, cancellationToken).ConfigureAwait(false);
                return element;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "SoccerData request failed attempt {Attempt}/{RetryCount}", attempt, _options.RetryCount);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning(ex, "SoccerData request timeout attempt {Attempt}/{RetryCount}", attempt, _options.RetryCount);
            }

            if (attempt < _options.RetryCount)
            {
                var backoff = TimeSpan.FromSeconds(_options.RetryDelaySeconds * attempt);
                var jitter = Random.Shared.NextDouble() * 0.5 + 0.5; // 0.5..1.5
                await Task.Delay(TimeSpan.FromMilliseconds(backoff.TotalMilliseconds * jitter), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new SoccerDataException($"Failed to fetch {requestUri.AbsoluteUri} after {_options.RetryCount} attempts", lastException!);
    }
}
