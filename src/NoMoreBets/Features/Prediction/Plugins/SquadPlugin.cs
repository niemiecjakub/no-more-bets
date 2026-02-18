using System.ComponentModel;
using System.Text.Json;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.Rotowire.Model;

namespace NoMoreBets.Features.Prediction.Plugins;

/// <summary>
/// Plugin exposing squad availability and predicted lineups.
/// </summary>
public sealed class SquadPlugin(IMediator mediator)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  [KernelFunction("get_injuries")]
  [Description("Fetches injuried players for both teams.")]
  public async Task<string> GetInjuriesAsync(
      [Description("Home team name.")] string homeTeam,
      [Description("Away team name.")] string awayTeam,
      CancellationToken cancellationToken = default)
  {
    var matchup = await GetMatchupAsync(homeTeam, awayTeam, cancellationToken);
    var payload = new
    {
      homeTeam = matchup?.HomeTeamName ?? homeTeam,
      awayTeam = matchup?.AwayTeamName ?? awayTeam,
      homeInjuries = matchup?.HomeTeam.Injuries ?? [],
      awayInjuries = matchup?.AwayTeam.Injuries ?? []
    };

    return JsonSerializer.Serialize(payload, JsonOptions);
  }

  [KernelFunction("get_predicted_lineups")]
  [Description("Fetches predicted lineups for both teams from Rotowire.")]
  public async Task<string> GetPredictedLineupsAsync(
        [Description("Home team name.")] string homeTeam,
        [Description("Away team name.")] string awayTeam,
        CancellationToken cancellationToken = default)
  {
    var matchup = await GetMatchupAsync(homeTeam, awayTeam, cancellationToken);
    var payload = new
    {
      homeTeam = matchup?.HomeTeamName ?? homeTeam,
      awayTeam = matchup?.AwayTeamName ?? awayTeam,
      homeLineupType = matchup?.HomeTeam.LineupType.ToString() ?? "Unknown",
      awayLineupType = matchup?.AwayTeam.LineupType.ToString() ?? "Unknown",
      homePlayers = matchup?.HomeTeam.Players ?? [],
      awayPlayers = matchup?.AwayTeam.Players ?? []
    };

    return JsonSerializer.Serialize(payload, JsonOptions);
  }

  private async Task<GameLineup?> GetMatchupAsync(string homeTeam, string awayTeam, CancellationToken cancellationToken)
  {
    var lineups = await mediator.Send(new GetRotowireLineupsQuery(), cancellationToken);
    return lineups.FirstOrDefault(lineup =>
        IsSameTeam(lineup.HomeTeamName, homeTeam) &&
        IsSameTeam(lineup.AwayTeamName, awayTeam));
  }

  private static bool IsSameTeam(string left, string right) =>
      left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
      right.Contains(left, StringComparison.OrdinalIgnoreCase);
}
