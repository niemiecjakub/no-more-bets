using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;
using NoMoreBets.Infrastructure.XApi.Models;

namespace NoMoreBets.Infrastructure.XApi;

public sealed class XApiClient : IXApiService
{
  private readonly HttpClient _httpClient;
  private readonly IOptions<XApiOptions> _options;
  private readonly ILogger<XApiClient> _logger;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
  };

  public XApiClient(HttpClient httpClient, IOptions<XApiOptions> options, ILogger<XApiClient> logger)
  {
    _httpClient = httpClient;
    _options = options;
    _logger = logger;
  }

  public async Task<CreateXPostResult> CreateXPostAsync(CreateXPostRequest request, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);
    var text = request.Text.Trim();
    if (text.Length == 0)
      throw new ArgumentException("Post text is required.", nameof(request));
    if (text.Length > CreateXPostRequest.MaxTweetTextLength)
      throw new ArgumentException(
        $"Post text must be at most {CreateXPostRequest.MaxTweetTextLength} characters.",
        nameof(request));

    _options.Value.EnsureOAuthConfigured();

    var payload = new TweetCreatePayload { Text = text };

    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/2/tweets");
    httpRequest.Content = JsonContent.Create(payload, options: JsonOptions, mediaType: new MediaTypeHeaderValue("application/json"));

    using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

    if (response.StatusCode == System.Net.HttpStatusCode.Created)
    {
      var created = JsonSerializer.Deserialize<TweetCreateResponsePayload>(body, JsonOptions);
      if (created?.Data is null || string.IsNullOrEmpty(created.Data.Id))
      {
        _logger.LogWarning(
          "X API returned Created for route {Route} but response tweet data was missing. BodyLength: {BodyLength}",
          "/2/tweets",
          body.Length);
        throw new XApiPostsException(502, "X API returned 201 but the response body was missing tweet data.");
      }

      return new CreateXPostResult { Id = created.Data.Id, Text = created.Data.Text ?? "" };
    }

    _logger.LogError(
      "X API post creation failed for route {Route} with status code {StatusCode}. BodyLength: {BodyLength}",
      "/2/tweets",
      (int)response.StatusCode,
      body.Length);
    var message = TryFormatErrorBody(body);
    throw new XApiPostsException((int)response.StatusCode, message);
  }

  private string TryFormatErrorBody(string body)
  {
    if (string.IsNullOrWhiteSpace(body))
      return "X API request failed with an empty error body.";

    try
    {
      var problem = JsonSerializer.Deserialize<ProblemPayload>(body, JsonOptions);
      if (problem is not null)
      {
        var parts = new[] { problem.Title, problem.Detail, problem.Status?.ToString() }
          .Where(s => !string.IsNullOrWhiteSpace(s))
          .ToArray();
        if (parts.Length > 0)
          return string.Join(" — ", parts);
      }
    }
    catch (JsonException)
    {
      _logger.LogWarning(
        "X API error body could not be parsed as ProblemPayload. Falling back to raw body formatting. BodyLength: {BodyLength}",
        body.Length);
    }

    return body.Length > 500 ? body[..500] + "…" : body;
  }
}
