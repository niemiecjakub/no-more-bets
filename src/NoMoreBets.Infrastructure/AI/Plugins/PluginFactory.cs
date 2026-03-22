using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class PluginFactory : IPluginFactory
{
  private readonly IServiceProvider _sp;
  public PluginFactory(IServiceProvider sp) => _sp = sp;

  public async Task<object> CreateMatchPluginAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var unitOfWork = _sp.GetRequiredService<IUnitOfWork>();
    var match = await unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken).ConfigureAwait(false)
      ?? throw new ArgumentException($"Match {matchId} not found.");

    return ActivatorUtilities.CreateInstance<MatchPlugin>(_sp, match);
  }

  public object CreateBettingPlugin()
  {
    return ActivatorUtilities.CreateInstance<BettingPlugin>(_sp);
  }

  public object CreateSearchPlugin()
  {
    return ActivatorUtilities.CreateInstance<SearchPlugin>(_sp);
  }

  public object CreateMemoriesPlugin()
  {
    return ActivatorUtilities.CreateInstance<MemoriesPlugin>(_sp);
  }
}
