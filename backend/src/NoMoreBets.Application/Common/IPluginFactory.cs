namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  object CreateMatchPlugin();
  object CreateBettingPlugin();
  object CreateInternetSearchPlugin();
  object CreateMemoriesPlugin();
  object CreateResearchBetPlugin(int matchId);
  object CreateBankrollPlugin();
  object CreateSocialMediaPlugin();
}
