using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateLineup;

/// <summary>Command to refresh Rotowire lineups (scrape and persist to database).</summary>
public record UpdateLineupsCommand : IRequest<Unit>;

/// <summary>
/// Handles <see cref="UpdateLineupsCommand"/> by scraping RotoWire and upserting lineups into the database.
/// </summary>
public class UpdateLineupsHandler(
  ILineupProvider lineupProvider,
  IMatchMatcher matchMatcher,
  IUnitOfWork unitOfWork,
  ILogger<UpdateLineupsHandler> logger) : IRequestHandler<UpdateLineupsCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  /// <inheritdoc />
  public async Task<Unit> Handle(UpdateLineupsCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName}: starting Rotowire lineups refresh",
      nameof(UpdateLineupsHandler));

    var lineups = await lineupProvider.GetSoccerLineupsAsync();

    var matchedCount = 0;
    var unmatchedCount = 0;
    var insertedCount = 0;
    var updatedCount = 0;

    foreach (var lineup in lineups)
    {
      var matchesOnDay = await unitOfWork.Matches.GetMatches(lineup.Date);

      var candidates = matchesOnDay
        .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
        .ToList();

      var matched = matchMatcher.FindBestMatch(lineup.HomeTeamName, lineup.AwayTeamName, candidates);
      if (matched == null)
      {
        unmatchedCount++;
        logger.LogWarning(
          "Handler {HandlerName} could not find match for Rotowire lineup {HomeTeam} vs {AwayTeam} on {GameDayUtc}",
          nameof(UpdateLineupsHandler),
          lineup.HomeTeamName,
          lineup.AwayTeamName,
          lineup.Date);
        continue;
      }

      matchedCount++;

      var homeTeamJson = JsonSerializer.Serialize(lineup.HomeTeam, JsonOptions);
      var awayTeamJson = JsonSerializer.Serialize(lineup.AwayTeam, JsonOptions);

      var entity = await unitOfWork.Matches.GetLineup(matched.Id);

      if (entity == null)
      {
        entity = new Lineup { MatchId = matched.Id };
        await unitOfWork.Matches.AddLineup(entity);
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

    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Handler {HandlerName} completed Rotowire lineups refresh. Matched={MatchedCount}, Unmatched={UnmatchedCount}, Inserted={InsertedCount}, Updated={UpdatedCount}",
      nameof(UpdateLineupsHandler),
      matchedCount,
      unmatchedCount,
      insertedCount,
      updatedCount);
    return Unit.Value;
  }
}
