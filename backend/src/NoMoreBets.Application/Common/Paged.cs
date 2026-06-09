namespace NoMoreBets.Application.Common;

public record Paged<T>(
  IReadOnlyList<T> Items,
  bool HasMore,
  DateTime? NextCursorAt,
  int? NextCursorId);

public static class PagedFactory
{
  public static Paged<T> Create<T>(
    IReadOnlyList<T> items,
    bool hasMore,
    Func<T, DateTime> getSortAt,
    Func<T, int> getId) =>
    new(
      items,
      hasMore,
      hasMore && items.Count > 0 ? getSortAt(items[^1]) : null,
      hasMore && items.Count > 0 ? getId(items[^1]) : null);
}
