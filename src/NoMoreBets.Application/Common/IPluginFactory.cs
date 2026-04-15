namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  object CreateMatchPlugin();
  object CreateBettingPlugin();
  object CreateAgentBettingPlugin();
  object CreateSearchPlugin();
  object CreateMemoriesPlugin();
  object CreateAgentInternetResearchPlugin();
  object CreateAgentResearchPlugin();
  object CreateAgentReflectionPlugin();
  object CreateBankrollPlugin();
}
