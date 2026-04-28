namespace NoMoreBets.Infrastructure.BackgroundJobs;

public interface IRecurringJobRegistry
{
  void Register(JobMetadata metadata);
  IReadOnlyCollection<JobMetadata> GetAll();
  JobMetadata? Get(string id);
}
