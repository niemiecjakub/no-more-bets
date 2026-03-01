namespace NoMoreBets.Domain.Matches;
public interface IMatchPreviewProvider
{
  public Task<MatchPreview> GetMatchPreviewAsync(int soccerdataMatchId);
}
