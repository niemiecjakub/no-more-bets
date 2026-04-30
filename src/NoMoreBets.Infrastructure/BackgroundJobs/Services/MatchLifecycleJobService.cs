using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class MatchLifecycleJobService(AppDbContext db, ILogger<MatchLifecycleJobService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task CloseStartingSoonMatches()
  {
    var now = DateTime.UtcNow;
    var cutoff = now.AddHours(2);

    var matchesToClose = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming && m.MatchDate <= cutoff)
      .ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {MatchCount} upcoming matches starting before cutoff {Cutoff}",
      nameof(CloseStartingSoonMatches),
      matchesToClose.Count,
      cutoff);

    if (matchesToClose.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no matches to close",
        nameof(CloseStartingSoonMatches));
      return;
    }

    foreach (var match in matchesToClose)
    {
      match.MatchStatus = MatchStatus.Finished;
    }

    await db.SaveChangesAsync();

    logger.LogInformation(
      "Job {JobName} closed {MatchCount} matches starting before cutoff {Cutoff}",
      nameof(CloseStartingSoonMatches),
      matchesToClose.Count,
      cutoff);
  }
}
