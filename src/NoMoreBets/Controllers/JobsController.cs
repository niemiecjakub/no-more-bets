using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Infrastructure.BackgroundJobs;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(IRecurringJobRegistry registry) : ControllerBase
{
  /// <summary>Visible recurring jobs grouped by group, with next run from Hangfire storage.</summary>
  [HttpGet("groups")]
  public ActionResult<IReadOnlyList<JobGroupDto>> GetJobGroups()
  {
    var now = DateTime.UtcNow;
    var recurringById = JobStorage.Current
      .GetConnection()
      .GetRecurringJobs()
      .ToDictionary(r => r.Id, StringComparer.Ordinal);

    var jobs = registry
      .GetAll()
      .Where(m => m.IsVisible)
      .Select(meta =>
      {
        recurringById.TryGetValue(meta.Id, out var recurring);
        var nextUtc = recurring?.NextExecution;
        TimeSpan? timeUntil = nextUtc is { } next && next > now
          ? next - now
          : null;

        return new
        {
          meta.Group,
          Job = new JobInfoDto(
            meta.Id,
            meta.Name,
            meta.Description,
            meta.CronExpression,
            nextUtc,
            timeUntil)
        };
      })
      .GroupBy(x => x.Group, StringComparer.Ordinal)
      .OrderBy(g => g.Key)
      .Select(g => new JobGroupDto(
        g.Key,
        g.Select(x => x.Job).OrderBy(j => j.Name, StringComparer.Ordinal).ToList()))
      .ToList();

    return Ok(jobs);
  }
}

public sealed record JobInfoDto(
  string Id,
  string Name,
  string Description,
  string CronExpression,
  DateTime? NextExecutionUtc,
  TimeSpan? TimeUntilNextRun);

public sealed record JobGroupDto(string Group, IReadOnlyList<JobInfoDto> Jobs);
