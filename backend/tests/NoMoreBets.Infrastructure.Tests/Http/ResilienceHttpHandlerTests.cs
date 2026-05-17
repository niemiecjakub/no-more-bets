using System.Net;
using FluentAssertions;
using NoMoreBets.Infrastructure.Http;
using Polly;

namespace NoMoreBets.Infrastructure.Tests.Http;

public class ResilienceHttpHandlerTests
{
  [Fact]
  public async Task SendAsync_WhenInnerReturnsSuccess_ReturnsResponse()
  {
    // Arrange
    var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().Build();
    var inner = new StubHandler(HttpStatusCode.OK);
    var sut = new ResilienceHttpHandler(pipeline) { InnerHandler = inner };
    using var invoker = new HttpMessageInvoker(sut);

    // Act
    var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/"), CancellationToken.None);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    inner.CallCount.Should().Be(1);
  }

  [Fact]
  public async Task SendAsync_WhenInnerReturnsTransientStatus_Throws()
  {
    // Arrange
    var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().Build();
    var inner = new StubHandler(HttpStatusCode.InternalServerError);
    var sut = new ResilienceHttpHandler(pipeline) { InnerHandler = inner };
    using var invoker = new HttpMessageInvoker(sut);

    // Act
    var act = async () => await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/"), CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<HttpRequestException>();
    inner.CallCount.Should().Be(1);
  }

  [Fact]
  public async Task SendAsync_WhenInnerReturns404_DoesNotThrow()
  {
    // Arrange
    var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().Build();
    var inner = new StubHandler(HttpStatusCode.NotFound);
    var sut = new ResilienceHttpHandler(pipeline) { InnerHandler = inner };
    using var invoker = new HttpMessageInvoker(sut);

    // Act
    var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/"), CancellationToken.None);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public void CreatePipeline_WithNullLogger_BuildsPipeline()
  {
    // Act
    var pipeline = ResilienceHttpHandler.CreatePipeline(logger: null);

    // Assert
    pipeline.Should().NotBeNull();
  }

  private sealed class StubHandler : HttpMessageHandler
  {
    private readonly HttpStatusCode _statusCode;
    public int CallCount;

    public StubHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      CallCount++;
      return Task.FromResult(new HttpResponseMessage(_statusCode));
    }
  }
}
