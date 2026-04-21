using MediatR;

namespace NoMoreBets.Application.Matches.BackfillRecentFotmobMatchDetails;

/// <summary>TEMP: One-shot backfill — enqueue via Hangfire only; remove when no longer needed.</summary>
public sealed record BackfillRecentFotmobMatchDetailsCommand : IRequest<BackfillRecentFotmobMatchDetailsResult>;

public sealed record BackfillRecentFotmobMatchDetailsResult(
  int ClubsSkippedUnmappedFotmob,
  int ClubsOverviewSucceeded,
  int ClubsOverviewFailed,
  int UniqueFotmobGameUrls,
  int UpdateMatchDetailsSucceeded,
  int UpdateMatchDetailsFailed);
