using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace NoMoreBets.Infrastructure.Http;

/// <summary>
/// Delegating handler that applies retry, timeout, and circuit breaker to HTTP requests using Polly.
/// Retries on transient failures (network errors, 5xx, 408); does not retry on 4xx (except 408).
/// </summary>
public sealed class ResilienceHttpHandler : DelegatingHandler
{
  private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

  public ResilienceHttpHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
  {
    _pipeline = pipeline;
  }

  protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
  {
    return await _pipeline.ExecuteAsync(async ct =>
    {
      var response = await base.SendAsync(request, ct).ConfigureAwait(false);
      if (IsTransientFailure(response.StatusCode))
      {
        response.Dispose();
        throw new HttpRequestException($"Transient failure: {response.StatusCode}");
      }
      return response;
    }, cancellationToken).ConfigureAwait(false);
  }

  private static bool IsTransientFailure(HttpStatusCode statusCode)
  {
    return (int)statusCode >= 500 || statusCode == HttpStatusCode.RequestTimeout; // 408
  }

  /// <summary>
  /// Builds a standard resilience pipeline for HTTP: retry (3 attempts, exponential backoff),
  /// circuit breaker (50% failure ratio, 5 min throughput, 30s break), timeout (15s).
  /// </summary>
  public static ResiliencePipeline<HttpResponseMessage> CreatePipeline(ILogger? logger = null)
  {
    var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
    {
      MaxRetryAttempts = 3,
      BackoffType = DelayBackoffType.Exponential,
      UseJitter = true,
      Delay = TimeSpan.FromSeconds(1),
      ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
          .Handle<HttpRequestException>()
          .Handle<TaskCanceledException>(ex => ex.InnerException is not OperationCanceledException),
      OnRetry = args =>
      {
        logger?.LogWarning(args.Outcome.Exception, "HTTP request retry {Attempt}/4", args.AttemptNumber + 1);
        return ValueTask.CompletedTask;
      }
    };

    var circuitBreakerOptions = new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
      FailureRatio = 0.5,
      MinimumThroughput = 5,
      BreakDuration = TimeSpan.FromSeconds(30),
      ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
          .Handle<HttpRequestException>()
          .Handle<TaskCanceledException>(),
      OnOpened = _ =>
      {
        logger?.LogWarning("HTTP circuit breaker opened for 30s");
        return ValueTask.CompletedTask;
      },
      OnClosed = _ => ValueTask.CompletedTask,
      OnHalfOpened = _ => ValueTask.CompletedTask
    };

    var timeoutOptions = new TimeoutStrategyOptions
    {
      Timeout = TimeSpan.FromSeconds(15)
    };

    return new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(retryOptions)
        .AddCircuitBreaker(circuitBreakerOptions)
        .AddTimeout(timeoutOptions)
        .Build();
  }
}
