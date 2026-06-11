using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public sealed class MatchResearchOutput
{
  [Description(
    "A concise pre-match intelligence summary (2-5 paragraphs) covering the most important findings about the fixture. Focus on team strength, recent context, tactical themes, availability news, and other factors relevant to understanding the match. Do not provide betting recommendations, value judgments, picks, leans, or confidence ratings."
  )]
  public required string MatchOverview { get; init; }

  [Description(
    "A list of the most important factual observations and match-relevant insights discovered during research. Include notable form trends, lineup expectations, injuries, tactical factors, scheduling context, statistical patterns, market context, or other meaningful findings. Keep each item concise and informational."
  )]
  public required List<string> KeyPoints { get; init; }

  [Description(
    "A list of unresolved questions, uncertainties, missing information, or risk factors that could materially affect interpretation of the match. Examples include questionable player availability, expected rotation, incomplete lineup information, conflicting reports, limited sample sizes, or unreliable data."
  )]
  public required List<string> RisksAndUnknowns { get; init; }
}
