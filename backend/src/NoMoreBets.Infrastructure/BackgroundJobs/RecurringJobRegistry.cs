using System.Collections.Concurrent;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class RecurringJobRegistry
{
  private readonly ConcurrentDictionary<string, JobMetadata> _jobs = new(StringComparer.Ordinal);

  public void Register(JobMetadata metadata)
  {
    _jobs[metadata.Id] = metadata;
  }

  public IReadOnlyCollection<JobMetadata> GetAll() => _jobs.Values.ToList();

  public JobMetadata? Get(string id) =>
    _jobs.TryGetValue(id, out var meta) ? meta : null;
}
