using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.Persistence;

public sealed class DocumentChunkIndexInterceptor(IDocumentChunkIndexScheduler scheduler) : SaveChangesInterceptor
{
  private readonly List<(string SourceType, object Entity)> _pending = [];

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    Collect(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    Collect(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
  {
    EnqueuePending();
    return base.SavedChanges(eventData, result);
  }

  public override ValueTask<int> SavedChangesAsync(
    SaveChangesCompletedEventData eventData,
    int result,
    CancellationToken cancellationToken = default)
  {
    EnqueuePending();
    return base.SavedChangesAsync(eventData, result, cancellationToken);
  }

  public override void SaveChangesFailed(DbContextErrorEventData eventData)
  {
    _pending.Clear();
    base.SaveChangesFailed(eventData);
  }

  public override Task SaveChangesFailedAsync(
    DbContextErrorEventData eventData,
    CancellationToken cancellationToken = default)
  {
    _pending.Clear();
    return base.SaveChangesFailedAsync(eventData, cancellationToken);
  }

  private void Collect(DbContext? context)
  {
    _pending.Clear();
    if (context is null)
      return;

    foreach (var entry in context.ChangeTracker.Entries())
      DocumentChunkIndexChangeCollector.AddFromEntry(_pending, entry.Entity, entry.State);
  }

  private void EnqueuePending()
  {
    foreach (var (sourceType, sourceId) in DocumentChunkIndexChangeCollector.ResolveIds(_pending))
      scheduler.Enqueue(sourceType, sourceId);

    _pending.Clear();
  }
}
