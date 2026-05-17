using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateLineup;

/// <summary>Command to refresh lineups from RotoWire for one league (must exist and be RotoWire-supported).</summary>
/// <param name="LeagueId">League to refresh.</param>
public record UpdateLineupsCommand(int LeagueId) : IRequest<Unit>;

/// <summary>
/// Handles <see cref="UpdateLineupsCommand"/> by scraping RotoWire (per-league URLs in <c>RotowireScraper</c>) and upserting matched lineups into the database.
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
      "Handling {HandlerName}: starting RotoWire lineups refresh for league {LeagueId}",
      nameof(UpdateLineupsHandler),
      request.LeagueId);

    var supported = lineupProvider.SupportedLeagueSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var league = await unitOfWork.Leagues.GetByIdAsync(request.LeagueId, cancellationToken).ConfigureAwait(false);
    if (league is null)
    {
      logger.LogWarning(
        "Handler {HandlerName}: league {LeagueId} not found; skipping.",
        nameof(UpdateLineupsHandler),
        request.LeagueId);
      return Unit.Value;
    }

    if (!supported.Contains(league.Slug))
    {
      logger.LogWarning(
        "Handler {HandlerName}: league {LeagueId} (slug '{Slug}') is not supported for RotoWire lineups. Supported slugs: {SupportedSlugs}.",
        nameof(UpdateLineupsHandler),
        request.LeagueId,
        league.Slug,
        string.Join(", ", supported));
      return Unit.Value;
    }

    var matchedCount = 0;
    var unmatchedCount = 0;
    var insertedCount = 0;
    var updatedCount = 0;

    IReadOnlyList<GameLineup> lineups;
    try
    {
      lineups = await lineupProvider.GetSoccerLineupsAsync(league.Slug, cancellationToken).ConfigureAwait(false);
    }
    catch (NotSupportedException ex)
    {
      logger.LogWarning(
        ex,
        "Handler {HandlerName}: RotoWire lineups not supported for league {LeagueId}.",
        nameof(UpdateLineupsHandler),
        league.Id);
      return Unit.Value;
    }
    catch (ArgumentException ex)
    {
      logger.LogWarning(
        ex,
        "Handler {HandlerName}: invalid league slug '{LeagueSlug}' (id {LeagueId}).",
        nameof(UpdateLineupsHandler),
        league.Slug,
        league.Id);
      return Unit.Value;
    } 

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
          "Handler {HandlerName} could not find match for RotoWire lineup {HomeTeam} vs {AwayTeam} on {GameDayUtc}",
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
      "Handler {HandlerName} completed RotoWire lineups refresh for league {LeagueId}. Matched={MatchedCount}, Unmatched={UnmatchedCount}, Inserted={InsertedCount}, Updated={UpdatedCount}",
      nameof(UpdateLineupsHandler),
      request.LeagueId,
      matchedCount,
      unmatchedCount,
      insertedCount,
      updatedCount);
    return Unit.Value;
  }
}
