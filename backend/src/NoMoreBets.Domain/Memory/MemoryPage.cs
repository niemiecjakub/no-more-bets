namespace NoMoreBets.Domain.Memories;

public sealed record MemoryPage(IReadOnlyList<MemoryListItem> Items, bool HasMore);
