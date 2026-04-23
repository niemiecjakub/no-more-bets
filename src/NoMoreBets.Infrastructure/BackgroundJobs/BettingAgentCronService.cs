using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.GetMatchesAvailableForBetting;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire entry points for the betting agent: research scheduling, execution, and reflection phases.
/// </summary>
public sealed class BettingAgentCronService(
  Runner runner,
  IMediator mediator,
  IUnitOfWork unitOfWork,
  ILogger<BettingAgentCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunResearchScheduleAsync()
  {
    logger.LogInformation("Starting scheduled research agent phase");
    var matches = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(), CancellationToken.None)
      .ConfigureAwait(false);

    for (var i = 0; i < matches.Count; i++)
    {
      var matchId = matches[i].Id;
      var delay = TimeSpan.FromMinutes(i * 5);
      BackgroundJob.Schedule<BettingAgentCronService>(service => service.RunResearchPhaseForMatchAsync(matchId), delay);
    }

    logger.LogInformation("Finished scheduled research agent phase");
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task RunResearchPhaseForMatchAsync(int matchId)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(matchId, CancellationToken.None).ConfigureAwait(false);

    if (match is null)
    {
      logger.LogWarning("Skipping research phase because match {MatchId} was not found", matchId);
      return;
    }

    await runner.RunResearchPhaseAsync(match, CancellationToken.None).ConfigureAwait(false);
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task RunBettingExecutionAsync()
  {
    var matches = await mediator
      .Send(new GetMatchesAvailableForBettingQuery(), CancellationToken.None)
      .ConfigureAwait(false);
    if (matches.Count == 0)
    {
      logger.LogInformation("Skipping scheduled betting execution agent phase: no matches available for betting");
      return;
    }

    logger.LogInformation("Starting scheduled betting execution agent phase");
    await runner.RunBettingExecutionPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled betting execution agent phase");
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task RunReflectionAsync()
  {
    logger.LogInformation("Starting scheduled reflection agent phase");
    await runner.RunReflectionPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled reflection agent phase");
  }
}
