namespace NoMoreBets.Domain.Memories;

public sealed record MemoryListItem(
  int Id,
  string Name,
  string? Description,
  string Content,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  DateTime? DeletedAt);
