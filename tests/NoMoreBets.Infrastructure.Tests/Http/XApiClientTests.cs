using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;
using NoMoreBets.Infrastructure.XApi;

namespace NoMoreBets.Infrastructure.Tests.Http;

public class XApiClientTests
{
  private static XApiOptions TestOAuthOptions => new()
  {
    ConsumerKey = "ck",
    ConsumerSecret = "cs",
    AccessToken = "at",
    AccessTokenSecret = "ats"
  };

  private static XApiClient CreateClient(
    Func<HttpRequestMessage, HttpResponseMessage> responder,
    XApiOptions? oauthOptions = null)
  {
    oauthOptions ??= TestOAuthOptions;
    var mock = new MockHandler(responder);
    var oauth = new XApiOAuth1MessageHandler(Options.Create(oauthOptions)) { InnerHandler = mock };
    var httpClient = new HttpClient(oauth) { BaseAddress = new Uri("https://api.x.com") };
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    return new XApiClient(httpClient, Options.Create(oauthOptions));
  }

  [Fact]
  public async Task CreateXPostAsync_SendsExpectedRequestAndMapsResponse()
  {
    HttpRequestMessage? captured = null;
    string? requestBody = null;
    const string responseJson = """{"data":{"id":"123456789","text":"hello from x"}}""";

    var client = CreateClient(req =>
    {
      captured = req;
      requestBody = req.Content is null
        ? null
        : req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
      return new HttpResponseMessage(HttpStatusCode.Created)
      {
        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
      };
    });

    var result = await client.CreateXPostAsync(new CreateXPostRequest { Text = "hello from x" });

    captured.Should().NotBeNull();
    captured!.Method.Should().Be(HttpMethod.Post);
    captured.RequestUri.Should().NotBeNull();
    captured.RequestUri!.ToString().Should().EndWith("/2/tweets");

    captured.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
    var auth = string.Join("", authValues!);
    auth.Should().StartWith("OAuth ");
    auth.Should().Contain("oauth_consumer_key=\"ck\"");
    auth.Should().Contain("oauth_token=\"at\"");
    auth.Should().Contain("oauth_signature_method=\"HMAC-SHA1\"");
    auth.Should().Contain("oauth_signature=\"");

    requestBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(requestBody!);
    doc.RootElement.GetProperty("text").GetString().Should().Be("hello from x");

    result.Id.Should().Be("123456789");
    result.Text.Should().Be("hello from x");
  }

  [Fact]
  public async Task CreateXPostAsync_WhenXReturnsError_ThrowsXApiPostsExceptionWithStatus()
  {
    const string problem = """{"title":"Invalid Request","detail":"Something wrong","status":400}""";
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
    {
      Content = new StringContent(problem, Encoding.UTF8, "application/problem+json")
    });

    var act = () => client.CreateXPostAsync(new CreateXPostRequest { Text = "x" });

    var ex = await act.Should().ThrowAsync<XApiPostsException>();
    ex.Which.StatusCode.Should().Be(400);
    ex.Which.Message.Should().Contain("Invalid Request");
  }

  [Fact]
  public async Task CreateXPostAsync_WhenTextExceeds280Characters_ThrowsArgumentException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Created));
    var longText = new string('a', CreateXPostRequest.MaxTweetTextLength + 1);

    var act = () => client.CreateXPostAsync(new CreateXPostRequest { Text = longText });

    (await act.Should().ThrowAsync<ArgumentException>())
      .Which.ParamName.Should().Be("request");
  }

  [Fact]
  public async Task CreateXPostAsync_WhenOAuthCredentialsMissing_ThrowsInvalidOperationException()
  {
    var emptyOAuth = new XApiOptions();
    var httpClient = new HttpClient(new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
    {
      BaseAddress = new Uri("https://api.x.com")
    };
    var client = new XApiClient(httpClient, Options.Create(emptyOAuth));

    var act = () => client.CreateXPostAsync(new CreateXPostRequest { Text = "hi" });

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*OAuth 1.0a*");
  }

  private sealed class MockHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public MockHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
      _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      return Task.FromResult(_responder(request));
    }
  }
}
