using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>Command to refresh Rotowire lineups (scrape and persist to database).</summary>
public record RefreshRotowireLineupsCommand : IRequest<Unit>;

/// <summary>
/// Handles <see cref="RefreshRotowireLineupsCommand"/> by scraping RotoWire and upserting lineups into the database.
/// </summary>
public class RefreshRotowireLineupsHandler(
  RotowireScraper scraper,
  AppDbContext db,
  IMatchMatcher matchMatcher,
  ILogger<RefreshRotowireLineupsHandler> logger) : IRequestHandler<RefreshRotowireLineupsCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <inheritdoc />
  public async Task<Unit> Handle(RefreshRotowireLineupsCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName}: starting Rotowire lineups refresh",
      nameof(RefreshRotowireLineupsHandler));

    var lineups = await scraper.GetSoccerLineupsAsync(cancellationToken).ConfigureAwait(false);

    var matchedCount = 0;
    var unmatchedCount = 0;
    var insertedCount = 0;
    var updatedCount = 0;

    foreach (var lineup in lineups)
    {
      var gameDayUtc = DateTime.SpecifyKind(lineup.Date, DateTimeKind.Utc).Date;
      var matchesOnDay = await db.Match
        .Where(m => m.MatchDate.Date == gameDayUtc)
        .Include(m => m.HomeClub)
        .Include(m => m.AwayClub)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

      var candidates = matchesOnDay
        .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
        .ToList();

      var matched = matchMatcher.FindBestMatch(lineup.HomeTeamName, lineup.AwayTeamName, candidates);
      if (matched == null)
      {
        unmatchedCount++;
        logger.LogWarning(
          "Handler {HandlerName} could not find match for Rotowire lineup {HomeTeam} vs {AwayTeam} on {GameDayUtc}",
          nameof(RefreshRotowireLineupsHandler),
          lineup.HomeTeamName,
          lineup.AwayTeamName,
          gameDayUtc);
        continue;
      }

      matchedCount++;

      var homeTeamJson = JsonSerializer.Serialize(lineup.HomeTeam, JsonOptions);
      var awayTeamJson = JsonSerializer.Serialize(lineup.AwayTeam, JsonOptions);

      var entity = await db.Lineup.SingleOrDefaultAsync(l => l.MatchId == matched.Id, cancellationToken);

      if (entity == null)
      {
        entity = new Lineup { MatchId = matched.Id };
        db.Lineup.Add(entity);
        insertedCount++;
      }
      else
      {
        updatedCount++;
      }

      entity.HomeTeamJson = homeTeamJson;
      entity.AwayTeamJson = awayTeamJson;
      entity.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Handler {HandlerName} completed Rotowire lineups refresh. Matched={MatchedCount}, Unmatched={UnmatchedCount}, Inserted={InsertedCount}, Updated={UpdatedCount}",
      nameof(RefreshRotowireLineupsHandler),
      matchedCount,
      unmatchedCount,
      insertedCount,
      updatedCount);
    return Unit.Value;
  }
}
