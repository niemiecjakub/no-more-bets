namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  object CreateMatchPlugin();
  object CreateBettingPlugin();
  object CreateSearchPlugin();
  object CreateMemoriesPlugin();
  object CreateBankrollPlugin();
}
