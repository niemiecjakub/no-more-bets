using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>
/// Handles <see cref="GetRotowireLineupsQuery"/> by delegating to <see cref="IRotowireScraper"/>.
/// </summary>
public class GetRotowireLineupsHandler(
  IRotowireScraper scraper,
  AppDbContext db,
  IMatchMatcher matchMatcher) : IRequestHandler<GetRotowireLineupsQuery, IReadOnlyList<GameLineup>>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <inheritdoc />
  public async Task<IReadOnlyList<GameLineup>> Handle(GetRotowireLineupsQuery request, CancellationToken cancellationToken)
  {
    var lineups = await scraper.GetSoccerLineupsAsync(cancellationToken).ConfigureAwait(false);

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
        continue;
      }

      var homeTeamJson = JsonSerializer.Serialize(lineup.HomeTeam, JsonOptions);
      var awayTeamJson = JsonSerializer.Serialize(lineup.AwayTeam, JsonOptions);

      var entity = await db.Lineup.SingleOrDefaultAsync(l => l.MatchId == matched.Id, cancellationToken);

      if (entity == null)
      {
        entity = new Lineup { MatchId = matched.Id };
        db.Lineup.Add(entity);
      }

      entity.HomeTeamJson = homeTeamJson;
      entity.AwayTeamJson = awayTeamJson;
      entity.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return lineups;
  }
}
