using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence;

internal static class DocumentChunkIndexChangeCollector
{
  public static void AddFromEntry(
    List<(string SourceType, object Entity)> pending,
    object entity,
    EntityState state)
  {
    if (state is not (EntityState.Added or EntityState.Modified))
      return;

    switch (entity)
    {
      case Match or Lineup or MatchEvent:
        pending.Add((DocumentChunkSourceType.Match, entity));
        break;
      case MatchAnalysis:
        pending.Add((DocumentChunkSourceType.MatchAnalysis, entity));
        break;
    }
  }

  public static IReadOnlyList<(string SourceType, int SourceId)> ResolveIds(
    IEnumerable<(string SourceType, object Entity)> pending)
  {
    var seen = new HashSet<(string SourceType, int SourceId)>();
    var result = new List<(string SourceType, int SourceId)>();

    foreach (var (sourceType, entity) in pending)
    {
      var sourceId = entity switch
      {
        Match match => match.Id,
        MatchAnalysis analysis => analysis.Id,
        Lineup lineup => lineup.MatchId,
        MatchEvent matchEvent => matchEvent.MatchId,
        _ => 0
      };

      if (sourceId <= 0)
        continue;

      if (seen.Add((sourceType, sourceId)))
        result.Add((sourceType, sourceId));
    }

    return result;
  }
}
