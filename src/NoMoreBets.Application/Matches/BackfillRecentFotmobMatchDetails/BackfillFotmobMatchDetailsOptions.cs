namespace NoMoreBets.Application.Matches.BackfillRecentFotmobMatchDetails;

public sealed class BackfillFotmobMatchDetailsOptions
{
  public const string SectionName = "BackfillFotmobMatchDetails";

  /// <summary>Pause after each FotMob scrape (club overview or match details) to reduce Playwright contention.</summary>
  public int DelayBetweenFotmobRequestsMs { get; set; } = 3000;
}
