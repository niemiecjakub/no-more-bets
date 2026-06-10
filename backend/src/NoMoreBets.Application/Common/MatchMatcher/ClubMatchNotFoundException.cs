namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>Thrown when a team name cannot be matched to any club in the league.</summary>
public sealed class ClubMatchNotFoundException(string teamName, string message) : Exception(message)
{
  public string TeamName { get; } = teamName;
}
