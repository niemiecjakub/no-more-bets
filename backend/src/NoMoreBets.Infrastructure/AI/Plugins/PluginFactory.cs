using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class PluginFactory : IPluginFactory
{
  private readonly IServiceProvider _sp;

  public PluginFactory(IServiceProvider sp)
  {
    _sp = sp;
  }

  public MatchPlugin CreateMatchPlugin() =>
    ActivatorUtilities.CreateInstance<MatchPlugin>(_sp);

  public BettingPlugin CreateBettingPlugin() =>
    ActivatorUtilities.CreateInstance<BettingPlugin>(_sp);

  public InternetSearchPlugin CreateInternetSearchPlugin() =>
    ActivatorUtilities.CreateInstance<InternetSearchPlugin>(_sp);

  public ResearchBetPlugin CreateResearchBetPlugin(int matchId) =>
    ActivatorUtilities.CreateInstance<ResearchBetPlugin>(_sp, matchId);

  public SocialMediaPlugin CreateSocialMediaPlugin() =>
    ActivatorUtilities.CreateInstance<SocialMediaPlugin>(_sp);

  object IPluginFactory.CreateMatchPlugin() => CreateMatchPlugin();
  object IPluginFactory.CreateBettingPlugin() => CreateBettingPlugin();
  object IPluginFactory.CreateInternetSearchPlugin() => CreateInternetSearchPlugin();
  object IPluginFactory.CreateResearchBetPlugin(int matchId) => CreateResearchBetPlugin(matchId);
  object IPluginFactory.CreateSocialMediaPlugin() => CreateSocialMediaPlugin();
}
