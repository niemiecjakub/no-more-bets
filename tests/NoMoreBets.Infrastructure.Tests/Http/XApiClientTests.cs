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
  private static XApiClient CreateClient(
    Func<HttpRequestMessage, HttpResponseMessage> responder,
    string bearerToken = "test-bearer-token")
  {
    var handler = new MockHandler(responder);
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.x.com") };
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrWhiteSpace(bearerToken))
      httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
    var options = Options.Create(new XApiOptions { BearerToken = bearerToken });
    return new XApiClient(httpClient, options);
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
    captured.Headers.Authorization.Should().NotBeNull();
    captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
    captured.Headers.Authorization.Parameter.Should().Be("test-bearer-token");

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
  public async Task CreateXPostAsync_WhenBearerMissing_ThrowsInvalidOperationException()
  {
    var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.x.com") };
    var options = Options.Create(new XApiOptions { BearerToken = "" });
    var client = new XApiClient(httpClient, options);

    var act = () => client.CreateXPostAsync(new CreateXPostRequest { Text = "hi" });

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BearerToken*");
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
