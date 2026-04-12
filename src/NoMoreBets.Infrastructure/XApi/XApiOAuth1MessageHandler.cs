using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace NoMoreBets.Infrastructure.XApi;

/// <summary>
/// Signs outgoing X (Twitter) API requests with OAuth 1.0a user-context (HMAC-SHA1) and sets the Authorization header.
/// </summary>
public sealed class XApiOAuth1MessageHandler : DelegatingHandler
{
  private readonly XApiOptions _options;

  public XApiOAuth1MessageHandler(IOptions<XApiOptions> options)
  {
    ArgumentNullException.ThrowIfNull(options);
    _options = options.Value;
  }

  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    ApplyOAuthHeader(request);
    return base.SendAsync(request, cancellationToken);
  }

  private void ApplyOAuthHeader(HttpRequestMessage request)
  {
    if (!_options.IsOAuthConfigured)
      return;

    var consumerKey = _options.ConsumerKey.Trim();
    var consumerSecret = _options.ConsumerSecret.Trim();
    var accessToken = _options.AccessToken.Trim();
    var accessTokenSecret = _options.AccessTokenSecret.Trim();

    var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required for OAuth 1.0a signing.");
    if (!uri.IsAbsoluteUri)
      throw new InvalidOperationException("Request URI must be absolute for OAuth 1.0a signing.");

    var nonce = CreateNonce();
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    var oauthParams = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["oauth_consumer_key"] = consumerKey,
      ["oauth_nonce"] = nonce,
      ["oauth_signature_method"] = "HMAC-SHA1",
      ["oauth_timestamp"] = timestamp,
      ["oauth_token"] = accessToken,
      ["oauth_version"] = "1.0"
    };

    foreach (var (name, value) in ParseQueryParameters(uri.Query))
      oauthParams[name] = value;

    var signature = ComputeSignature(request.Method, uri, oauthParams, consumerSecret, accessTokenSecret);
    oauthParams["oauth_signature"] = signature;

    var headerValue = BuildAuthorizationHeaderValue(oauthParams);
    request.Headers.TryAddWithoutValidation("Authorization", headerValue);
  }

  private static string CreateNonce()
  {
    Span<byte> bytes = stackalloc byte[16];
    RandomNumberGenerator.Fill(bytes);
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }

  private static IEnumerable<(string Name, string Value)> ParseQueryParameters(string query)
  {
    if (string.IsNullOrEmpty(query) || query == "?")
      yield break;

    var q = query.StartsWith('?') ? query.AsSpan(1) : query.AsSpan();
    foreach (var part in q.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
      var eq = part.IndexOf('=');
      if (eq < 0)
      {
        yield return (Uri.UnescapeDataString(part), "");
        continue;
      }

      var name = Uri.UnescapeDataString(part[..eq]);
      var value = eq < part.Length - 1 ? Uri.UnescapeDataString(part[(eq + 1)..]) : "";
      yield return (name, value);
    }
  }

  private static string ComputeSignature(
    HttpMethod method,
    Uri requestUri,
    IReadOnlyDictionary<string, string> parameters,
    string consumerSecret,
    string tokenSecret)
  {
    var baseUrl = NormalizeUrl(requestUri);
    var parameterString = NormalizeParameterString(parameters);
    var signatureBase = string.Join("&",
      Rfc3986Encode(method.Method.ToUpperInvariant()),
      Rfc3986Encode(baseUrl),
      Rfc3986Encode(parameterString));

    var signingKey = string.Join("&", Rfc3986Encode(consumerSecret), Rfc3986Encode(tokenSecret));
    using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
    return Convert.ToBase64String(hash);
  }

  private static string NormalizeUrl(Uri uri)
  {
    var scheme = uri.Scheme.ToLowerInvariant();
    var host = uri.Host.ToLowerInvariant();
    var port = uri.IsDefaultPort ? "" : ":" + uri.Port;
    var path = uri.AbsolutePath;
    if (string.IsNullOrEmpty(path))
      path = "/";
    return $"{scheme}://{host}{port}{path}";
  }

  private static string NormalizeParameterString(IReadOnlyDictionary<string, string> parameters)
  {
    var pairs = new List<(string EncodedName, string EncodedValue)>(parameters.Count);
    foreach (var kv in parameters)
    {
      if (string.Equals(kv.Key, "oauth_signature", StringComparison.Ordinal))
        continue;
      pairs.Add((Rfc3986Encode(kv.Key), Rfc3986Encode(kv.Value)));
    }

    pairs.Sort(static (a, b) =>
    {
      var c = string.CompareOrdinal(a.EncodedName, b.EncodedName);
      return c != 0 ? c : string.CompareOrdinal(a.EncodedValue, b.EncodedValue);
    });

    return string.Join("&", pairs.Select(p => $"{p.EncodedName}={p.EncodedValue}"));
  }

  private static string BuildAuthorizationHeaderValue(IReadOnlyDictionary<string, string> oauthParams)
  {
    var encodedPairs = oauthParams
      .Select(kv => (EncodedName: Rfc3986Encode(kv.Key), EncodedValue: Rfc3986Encode(kv.Value)))
      .OrderBy(p => p.EncodedName, StringComparer.Ordinal)
      .ThenBy(p => p.EncodedValue, StringComparer.Ordinal)
      .ToArray();

    var parts = new List<string>(encodedPairs.Length);
    foreach (var p in encodedPairs)
      parts.Add($"{p.EncodedName}=\"{p.EncodedValue}\"");

    return "OAuth " + string.Join(", ", parts);
  }

  private static string Rfc3986Encode(string value)
  {
    if (string.IsNullOrEmpty(value))
      return "";

    var bytes = Encoding.UTF8.GetBytes(value);
    var sb = new StringBuilder(bytes.Length * 3);
    foreach (var b in bytes)
    {
      if (IsUnreserved(b))
        sb.Append((char)b);
      else
        sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
    }

    return sb.ToString();
  }

  private static bool IsUnreserved(byte b) =>
    b is >= (byte)'a' and <= (byte)'z'
      or >= (byte)'A' and <= (byte)'Z'
      or >= (byte)'0' and <= (byte)'9'
      or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';
}
