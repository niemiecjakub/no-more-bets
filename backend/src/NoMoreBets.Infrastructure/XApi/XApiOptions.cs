namespace NoMoreBets.Infrastructure.XApi;

public sealed class XApiOptions
{
  public const string SectionName = "XApi";

  public string BearerToken { get; init; } = string.Empty;

  public string ConsumerKey { get; init; } = string.Empty;

  public string ConsumerSecret { get; init; } = string.Empty;

  public string AccessToken { get; init; } = string.Empty;

  public string AccessTokenSecret { get; init; } = string.Empty;

  public bool IsOAuthConfigured =>
    !string.IsNullOrWhiteSpace(ConsumerKey)
    && !string.IsNullOrWhiteSpace(ConsumerSecret)
    && !string.IsNullOrWhiteSpace(AccessToken)
    && !string.IsNullOrWhiteSpace(AccessTokenSecret);

  public void EnsureOAuthConfigured()
  {
    if (!IsOAuthConfigured)
      throw new InvalidOperationException(
        "XApi OAuth 1.0a is not configured. Set XApi:ConsumerKey, ConsumerSecret, AccessToken, and AccessTokenSecret.");
  }
}
