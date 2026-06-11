using System.ComponentModel;

namespace NoMoreBets.Domain.Matches.Dto;

public sealed class MatchResearchOutput
{
  [Description(
    "A synthesized pre-match intelligence narrative explaining how the match is expected to be played. Focus on interaction between teams, tactical setup, relative strength, and contextual factors. This should be a cohesive explanation, not a list of facts. Do not repeat details that appear in KeyPoints or RisksAndUnknowns."
  )]
  public required string MatchOverview { get; init; }

  [Description(
    "Atomic, non-redundant factual insights that support understanding of the match. Each item should contain a single idea (e.g. form trend, injury update, tactical tendency, statistical signal). Do not repeat or restate information already expressed in MatchOverview."
  )]
  public required List<string> KeyPoints { get; init; }

  [Description(
    "Only material uncertainties that could change interpretation of the match. Include missing lineup information, injury doubts, tactical unknowns, or conflicting reports. Exclude general information already captured in other sections."
  )]
  public required List<string> RisksAndUnknowns { get; init; }
}
