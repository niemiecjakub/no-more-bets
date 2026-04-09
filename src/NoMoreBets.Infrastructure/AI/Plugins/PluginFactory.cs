using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class PluginFactory : IPluginFactory
{
  private readonly IServiceProvider _sp;
  public PluginFactory(IServiceProvider sp) => _sp = sp;

  public object CreateMatchPlugin() =>
    ActivatorUtilities.CreateInstance<MatchPlugin>(_sp);

  public object CreateBettingPlugin()
  {
    return ActivatorUtilities.CreateInstance<BettingPlugin>(_sp);
  }

  public object CreateAgentBettingPlugin()
  {
    return ActivatorUtilities.CreateInstance<AgentBettingPlugin>(_sp);
  }

  public object CreateSearchPlugin()
  {
    return ActivatorUtilities.CreateInstance<SearchPlugin>(_sp);
  }

  public object CreateMemoriesPlugin()
  {
    return ActivatorUtilities.CreateInstance<MemoriesPlugin>(_sp);
  }

  public object CreateAgentResearchPlugin()
  {
    return ActivatorUtilities.CreateInstance<AgentResearchPlugin>(_sp);
  }

  public object CreateBankrollPlugin()
  {
    return ActivatorUtilities.CreateInstance<BankrollPlugin>(_sp);
  }
}
