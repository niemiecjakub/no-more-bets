using Hangfire;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class HangfireDocumentChunkIndexScheduler(IBackgroundJobClient jobClient) : IDocumentChunkIndexScheduler
{
  public void Enqueue(string sourceType, int sourceId)
  {
    jobClient.Enqueue<DocumentChunkIndexJobService>(
      job => job.IndexAsync(sourceType, sourceId));
  }
}
