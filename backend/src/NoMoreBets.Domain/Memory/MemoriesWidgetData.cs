namespace NoMoreBets.Domain.Memories;

public sealed record MemoriesWidgetData(
  int MemoriesCount,
  DateTime? LatestUpdatedAt,
  string? LatestName);
