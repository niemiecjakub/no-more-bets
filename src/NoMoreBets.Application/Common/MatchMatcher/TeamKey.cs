namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>
/// Order-independent key for matching teams (home/away). Normalized for lookup.
/// First and Second are the normalized team names in sorted order.
/// </summary>
public readonly record struct TeamKey
{
  public string First { get; }
  public string Second { get; }

  public TeamKey(string home, string away)
  {
    var h = (home ?? string.Empty).Trim().ToLowerInvariant();
    var a = (away ?? string.Empty).Trim().ToLowerInvariant();
    if (string.CompareOrdinal(h, a) <= 0)
    {
      First = h;
      Second = a;
    }
    else
    {
      First = a;
      Second = h;
    }
  }

  public override int GetHashCode() => HashCode.Combine(First, Second);

  public bool Equals(TeamKey other) => First == other.First && Second == other.Second;

  /// <summary>Search string for fuzzy matching (e.g. "team a vs team b").</summary>
  public string ToSearchString() => $"{First} vs {Second}";
}
