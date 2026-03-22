namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  Task<object> CreateMatchPluginAsync(int matchId, CancellationToken cancellationToken = default);
  object CreateBettingPlugin();
  object CreateSearchPlugin();
  object CreateMemoriesPlugin();
}
