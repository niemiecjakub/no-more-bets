using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public class MemoryRepository : IMemoryRepository
{
  private readonly AppDbContext _db;

  public MemoryRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task<IReadOnlyList<MemoryRecordListItem>> GetRecordsAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Memory
      .AsNoTracking()
      .OrderBy(m => m.Name)
      .Select(m => new MemoryRecordListItem(m.Name, m.Description, m.UpdatedAt))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<Memory?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await _db.Memory
      .OrderBy(m => m.Id)
      .FirstOrDefaultAsync(m => m.Name == name, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddAsync(Memory memory, CancellationToken cancellationToken = default)
  {
    await _db.Memory.AddAsync(memory, cancellationToken).ConfigureAwait(false);
  }
}
