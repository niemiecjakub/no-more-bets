namespace NoMoreBets.Domain.Memories;

public interface IMemoryRepository
{
  Task<IReadOnlyList<string>> GetNamesAsync(CancellationToken cancellationToken = default);
  Task<Memory?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
  Task AddAsync(Memory memory, CancellationToken cancellationToken = default);
}
