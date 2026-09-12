using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class UpcomingMatchesInternetResearchCronService(
  InternetResearchPhaseRunner internetResearchPhaseRunner,
  IMediator mediator,
  ILogger<UpcomingMatchesInternetResearchCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    var upcomingMatches = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), CancellationToken.None)
      .ConfigureAwait(false);

    if (upcomingMatches.Count == 0)
    {
      logger.LogInformation("Skipping upcoming internet research: no upcoming matches available");
      return;
    }

    logger.LogInformation("Starting scheduled upcoming matches internet research agent phase");
    await internetResearchPhaseRunner.RunAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled upcoming matches internet research agent phase");
  }
}
