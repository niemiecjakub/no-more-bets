namespace NoMoreBets.Domain.Memories;

public interface IMemoryRepository
{
  Task<IReadOnlyList<MemoryRecordListItem>> GetRecordsAsync(CancellationToken cancellationToken = default);
  Task<MemoryPage> GetPageAsync(
    int limit,
    DateTime? afterUpdatedAtUtc,
    int? afterId,
    bool includeDeleted = false,
    CancellationToken cancellationToken = default);
  Task<Memory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
  Task AddAsync(Memory memory, CancellationToken cancellationToken = default);
  Task<bool> SoftDeleteByNameAsync(string name, CancellationToken cancellationToken = default);
  Task<MemoriesWidgetData> GetActiveMemoriesWidgetAsync(CancellationToken cancellationToken = default);
}
