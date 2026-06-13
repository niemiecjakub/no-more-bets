using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues;

public record WorldCupGroupDefinition(
  string Code,
  string Label,
  IReadOnlyList<int> FotmobTeamIds,
  IReadOnlyList<string> ClubNames);

public sealed class WorldCupGroupRegistry
{
  public WorldCupGroupRegistry(IReadOnlyList<WorldCupGroupDefinition> groups)
  {
    Groups = groups;
    _groupByClubName = BuildGroupByClubNameLookup(groups);
  }

  public IReadOnlyList<WorldCupGroupDefinition> Groups { get; }

  private readonly Dictionary<string, WorldCupGroupDefinition> _groupByClubName;

  public WorldCupGroupDefinition? GetGroupForClubName(string clubName)
  {
    var effectiveName = ClubNameMatchHints.ResolveEffectiveName(clubName ?? string.Empty);
    if (string.IsNullOrWhiteSpace(effectiveName))
      return null;

    if (_groupByClubName.TryGetValue(NormalizeKey(effectiveName), out var group))
      return group;

    var folded = ClubNameMatchHints.FoldDiacritics(effectiveName);
    return _groupByClubName.GetValueOrDefault(NormalizeKey(folded));
  }

  public bool IsClubInGroup(string clubName, string groupCode)
  {
    var group = GetGroupForClubName(clubName);
    return group is not null
      && string.Equals(group.Code, groupCode, StringComparison.OrdinalIgnoreCase);
  }

  public bool IsWorldCupLeagueSlug(string slug) =>
    string.Equals(slug?.Trim(), League.FifaWorldCupSlug, StringComparison.OrdinalIgnoreCase);

  private static Dictionary<string, WorldCupGroupDefinition> BuildGroupByClubNameLookup(
    IReadOnlyList<WorldCupGroupDefinition> groups)
  {
    var lookup = new Dictionary<string, WorldCupGroupDefinition>(StringComparer.OrdinalIgnoreCase);
    foreach (var group in groups)
    {
      foreach (var name in group.ClubNames)
      {
        lookup[NormalizeKey(name)] = group;
        lookup[NormalizeKey(ClubNameMatchHints.FoldDiacritics(name))] = group;
      }
    }

    return lookup;
  }

  private static string NormalizeKey(string value) =>
    ClubNameMatchHints.FoldDiacritics(value.Trim()).ToLowerInvariant();
}
