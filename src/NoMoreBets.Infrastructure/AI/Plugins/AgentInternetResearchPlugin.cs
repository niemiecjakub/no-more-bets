using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentInternetResearchPlugin : AgentPluginBase
{
  private readonly MatchPlugin _matchPlugin;

  public AgentInternetResearchPlugin(
    MatchPlugin matchPlugin,
    InternetSearchPlugin searchPlugin,
    MemoriesPlugin memoriesPlugin,
    BankrollPlugin bankrollPlugin)
    : base(memoriesPlugin, searchPlugin, bankrollPlugin)
  {
    _matchPlugin = matchPlugin;
  }

  [KernelFunction]
  [Description("Retrieves matches for which bets can currently be placed.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    return await _matchPlugin.GetUpcomingMatchesAsync(cancellationToken).ConfigureAwait(false);
  }
}
