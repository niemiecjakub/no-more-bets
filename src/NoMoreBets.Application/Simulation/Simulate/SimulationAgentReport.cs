using System.ComponentModel;

namespace NoMoreBets.Application.Simulation.Simulate;

public sealed class SimulationAgentReport
{
  [Description("Short executive summary of what you did this run: fixtures checked, news highlights, and betting decision.")]
  public string Summary { get; set; } = string.Empty;

  [Description("True if you called PlaceBetSlip with at least one selection; false if you passed.")]
  public bool BetPlaced { get; set; }

  [Description("Extra context: diary file updates, risk notes, or why you skipped betting.")]
  public string Notes { get; set; } = string.Empty;
}
