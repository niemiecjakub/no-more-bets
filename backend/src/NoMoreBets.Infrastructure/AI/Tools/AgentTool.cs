using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Tools;

public readonly struct AgentTool
{
  private readonly Func<PluginToolContext, AITool> _resolve;

  internal AgentTool(Func<PluginToolContext, AITool> resolve) => _resolve = resolve;

  internal AITool Resolve(PluginToolContext context) => _resolve(context);
}

internal sealed class PluginToolContext(IServiceProvider serviceProvider)
{
  private MatchTool? _match;
  private BettingTool? _betting;
  private SocialMediaTool? _social;
  private readonly Dictionary<int, ResearchBetTool> _researchBets = new();

  public MatchTool Match =>
    _match ??= serviceProvider.GetRequiredService<MatchTool>();

  public BettingTool Betting =>
    _betting ??= serviceProvider.GetRequiredService<BettingTool>();

  public SocialMediaTool SocialMedia =>
    _social ??= serviceProvider.GetRequiredService<SocialMediaTool>();

  public ResearchBetTool ResearchBet(int matchId)
  {
    if (_researchBets.TryGetValue(matchId, out var plugin))
    {
      return plugin;
    }

    plugin = ActivatorUtilities.CreateInstance<ResearchBetTool>(serviceProvider, matchId);
    _researchBets[matchId] = plugin;
    return plugin;
  }
}
