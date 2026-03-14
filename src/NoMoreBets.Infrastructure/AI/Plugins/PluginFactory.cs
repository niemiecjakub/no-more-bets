using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class PluginFactory : IPluginFactory
{
  private readonly IServiceProvider _sp;
  public PluginFactory(IServiceProvider sp) => _sp = sp;

  public object CreateMatchPlugin(int matchId)
  {
    return ActivatorUtilities.CreateInstance<MatchPlugin>(_sp, matchId);
  }

  public object CreateBettingPlugin()
  {
    return ActivatorUtilities.CreateInstance<BettingPlugin>(_sp);
  }
}
