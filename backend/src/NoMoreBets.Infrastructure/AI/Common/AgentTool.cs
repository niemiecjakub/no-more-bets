using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Common;

public readonly struct AgentTool
{
  private readonly Func<PluginToolContext, AITool> _resolve;

  internal AgentTool(Func<PluginToolContext, AITool> resolve) => _resolve = resolve;

  internal AITool Resolve(PluginToolContext context) => _resolve(context);
}

internal sealed class PluginToolContext(IPluginFactory factory)
{
  private MatchPlugin? _match;
  private BettingPlugin? _betting;
  private SocialMediaPlugin? _social;
  private readonly Dictionary<int, ResearchBetPlugin> _researchBets = new();

  public MatchPlugin Match =>
    _match ??= (MatchPlugin)factory.CreateMatchPlugin();

  public BettingPlugin Betting =>
    _betting ??= (BettingPlugin)factory.CreateBettingPlugin();

  public SocialMediaPlugin SocialMedia =>
    _social ??= (SocialMediaPlugin)factory.CreateSocialMediaPlugin();

  public ResearchBetPlugin ResearchBet(int matchId)
  {
    if (_researchBets.TryGetValue(matchId, out var plugin))
    {
      return plugin;
    }

    plugin = (ResearchBetPlugin)factory.CreateResearchBetPlugin(matchId);
    _researchBets[matchId] = plugin;
    return plugin;
  }
}
