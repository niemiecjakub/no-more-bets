using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class UpcomingMatchesInternetResearchCronService(
  IAgentPhaseRunner agentPhaseRunner,
  IUnitOfWork unitOfWork,
  ILogger<UpcomingMatchesInternetResearchCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    var upcomingMatches = await unitOfWork.Matches
      .GetUpcomingMatchesAsync(CancellationToken.None)
      .ConfigureAwait(false);

    if (upcomingMatches.Count == 0)
    {
      logger.LogInformation("Skipping upcoming internet research: no upcoming matches available");
      return;
    }

    logger.LogInformation("Starting scheduled upcoming matches internet research agent phase");
    await agentPhaseRunner.RunUpcomingMatchesInternetResearchAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled upcoming matches internet research agent phase");
  }
}
