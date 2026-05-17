namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match odds.</summary>
public record Odds
{
  public MatchWinnerOdds MatchWinner { get; init; } = null!;
  public OverUnderOdds OverUnder { get; init; } = null!;
  public HandicapOdds Handicap { get; init; } = null!;
  public int? LastModifiedTimestamp { get; init; }
}

public record HandicapOdds
{
  public double? Market { get; init; }
  public double? Home { get; init; }
  public double? Away { get; init; }
}

public record MatchWinnerOdds
{
  public double? Home { get; init; }
  public double? Draw { get; init; }
  public double? Away { get; init; }
}

public record OverUnderOdds
{
  public double? Total { get; init; }
  public double? Over { get; init; }
  public double? Under { get; init; }
}

