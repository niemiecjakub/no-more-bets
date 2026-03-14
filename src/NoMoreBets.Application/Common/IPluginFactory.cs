namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  object CreateMatchPlugin(int matchId);
  object CreateBettingPlugin();
}
