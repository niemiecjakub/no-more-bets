using System.ComponentModel;

namespace NoMoreBets.Application.Matches.GetMatchPrediction;
public class StructuredMatchAnalysis
{
  [Description("Basic contextual information about the match")]
  public string Context { get; set; }

  [Description("Recent results, winning/losing streaks, and momentum comparison for both teams")]
  public string Form { get; set; }

  [Description("Key tactical insights derived from formations, lineups and recent performances")]
  public string Tactics { get; set; }

  [Description("Named absences with positional impact, and key players likely to influence the match")]
  public string Squad { get; set; }

  [Description("xG/xGA comparison, league positions, H2H record, and overall statistical edge")]
  public string Statistics { get; set; }

  [Description("Odds movement direction and magnitude, and what it implies about market sentiment")]
  public string Market { get; set; }

  [Description("Expected tempo, possession dynamic, and factors most likely to decide the result")]
  public string MatchProjection { get; set; }

  [Description("The match prediction with outcome, confidence and key reasoning")]
  public string Prediction { get; set; }
}
