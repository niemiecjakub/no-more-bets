using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NoMoreBets.Configuration;
using NoMoreBets.Infrastructure.BackgroundJobs;

namespace NoMoreBets.Controllers;

/// <summary>TEMP: One-shot maintenance triggers. Delete this controller after backfill is done.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class MaintenanceController(
  IWebHostEnvironment environment,
  IOptions<MaintenanceOptions> maintenanceOptions) : ControllerBase
{
  /// <summary>TEMP: Enqueues Hangfire job to scrape FotMob club overviews and UpdateMatchDetails for all recent game URLs (big five + Ekstraklasa).</summary>
  [HttpPost("backfill-fotmob-recent-match-details")]
  [ProducesResponseType(StatusCodes.Status202Accepted)]
  [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public IActionResult BackfillFotmobRecentMatchDetails()
  {
    if (!CanRunBackfill())
      return StatusCode(StatusCodes.Status403Forbidden);

    BackgroundJob.Enqueue<JobService>(
      (JobService js) => js.BackfillRecentFotmobMatchDetailsForBigFiveAndEkstraklasa());
    return Accepted(new { message = "Backfill job enqueued." });
  }

  private bool CanRunBackfill()
  {
    if (environment.IsDevelopment())
      return true;

    var secret = maintenanceOptions.Value.BackfillSecret;
    if (string.IsNullOrWhiteSpace(secret))
      return false;

    var header = Request.Headers["X-Backfill-Secret"].FirstOrDefault();
    return string.Equals(header, secret, StringComparison.Ordinal);
  }
}
