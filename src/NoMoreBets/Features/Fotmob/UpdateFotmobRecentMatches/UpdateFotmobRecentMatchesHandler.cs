using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Fotmob.UpdateFotmobRecentMatches;

/// <summary>Command to refresh Fotmob match details from a club's recent games: fetch overview, scrape details for new URLs, fuzzy-match to Match, and insert MatchDetails.</summary>
public record UpdateFotmobRecentMatchesCommand(int TeamId) : IRequest<Unit>;

/// <summary>
/// Handles <see cref="UpdateFotmobRecentMatchesCommand"/> by fetching club overview, scraping details for new match URLs,
/// fuzzy-matching to domain Match by date and club names, and inserting MatchDetails.
/// </summary>
public class UpdateFotmobRecentMatchesHandler(
  IMediator mediator,
  AppDbContext db,
  IMatchMatcher matchMatcher,
  ILogger<UpdateFotmobRecentMatchesHandler> logger) : IRequestHandler<UpdateFotmobRecentMatchesCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <inheritdoc />
  public async Task<Unit> Handle(UpdateFotmobRecentMatchesCommand request, CancellationToken cancellationToken)
  {
    var overview = await mediator.Send(new GetFotmobClubOverviewQuery(request.TeamId), cancellationToken).ConfigureAwait(false);
    var urls = overview.RecentGames.Select(g => g.GameUrl).Distinct().ToList();

    foreach (var gameUrl in urls)
    {
      if (await db.MatchDetails.AnyAsync(m => m.FotmobUrl == gameUrl, cancellationToken).ConfigureAwait(false))
        continue;

      MatchDetailsDto details;
      try
      {
        details = await mediator.Send(new GetFotmobMatchDetailsQuery(gameUrl), cancellationToken).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to scrape match details for {GameUrl}; skipping.", gameUrl);
        continue;
      }

      Match? matched = null;
      var matchDateUtc = details.MatchDate?.UtcDateTime.Date;
      if (matchDateUtc.HasValue)
      {
        var matchesOnDay = await db.Match
          .Where(m => m.MatchDate.Date == matchDateUtc.Value)
          .Include(m => m.HomeClub)
          .Include(m => m.AwayClub)
          .ToListAsync(cancellationToken)
          .ConfigureAwait(false);

        var candidates = matchesOnDay
          .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
          .ToList();

        matched = matchMatcher.FindBestMatch(details.HomeTeam, details.AwayTeam, candidates);
      }

      var fotmobDetailsJson = JsonSerializer.Serialize(details, JsonOptions);
      var entity = new MatchDetails
      {
        FotmobUrl = gameUrl,
        FotmobDetailsJson = fotmobDetailsJson,
        MatchId = matched?.Id
      };
      db.MatchDetails.Add(entity);
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return Unit.Value;
  }
}
