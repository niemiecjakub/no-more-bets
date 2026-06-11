namespace NoMoreBets.Application.Matches.GetMatchAgentResearch;

public record MatchResearchOutputDto(
  string MatchOverview,
  IReadOnlyList<string> KeyPoints,
  IReadOnlyList<string> RisksAndUnknowns);
