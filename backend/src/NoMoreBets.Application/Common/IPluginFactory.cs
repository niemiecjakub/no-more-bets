namespace NoMoreBets.Application.Common;

public interface IPluginFactory
{
  object CreateMatchPlugin();
  object CreateBettingPlugin();
  object CreateInternetSearchPlugin();
  object CreateResearchBetPlugin(int matchId);
  object CreateSocialMediaPlugin();
}
