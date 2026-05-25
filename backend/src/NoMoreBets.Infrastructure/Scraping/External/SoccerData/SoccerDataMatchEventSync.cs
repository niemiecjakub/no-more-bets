using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;
using SoccerDataMatchEvent = NoMoreBets.Application.Common.Dto.Matches.MatchEvent;
using SoccerDataPlayer = NoMoreBets.Application.Common.Dto.Matches.Player;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using DomainPlayer = NoMoreBets.Domain.Players.Player;
namespace NoMoreBets.Infrastructure.Scraping.External.SoccerData;

internal static class SoccerDataMatchEventSync
{
  public static async Task<int> AddMissingEventsAsync(
    AppDbContext db,
    DomainMatch match,
    IReadOnlyList<SoccerDataMatchEvent> soccerDataEvents,
    ILogger logger,
    CancellationToken cancellationToken = default)
  {
    if (soccerDataEvents.Count == 0)
      return 0;

    var domainEvents = new List<MatchEvent>();
    foreach (var soccerDataEvent in soccerDataEvents)
    {
      if (!TryParseMinute(soccerDataEvent.EventMinute, out var minute))
        continue;

      if (!TryResolveClubId(soccerDataEvent.Team, match.HomeClubId, match.AwayClubId, out var clubId))
      {
        logger.LogWarning(
          "Skipping SoccerData event with unknown team {Team} for MatchId={MatchId}",
          soccerDataEvent.Team,
          match.Id);
        continue;
      }

      if (soccerDataEvent.EventType.Equals("substitution", StringComparison.OrdinalIgnoreCase))
      {
        var playerIn = await GetOrCreatePlayerAsync(db, soccerDataEvent.PlayerIn, cancellationToken)
          .ConfigureAwait(false);
        var playerOut = await GetOrCreatePlayerAsync(db, soccerDataEvent.PlayerOut, cancellationToken)
          .ConfigureAwait(false);

        if (playerIn is not null)
        {
          domainEvents.Add(MatchEvent.Create(match.Id, clubId, playerIn, MatchEventType.SubstitutionIn, minute));
        }

        if (playerOut is not null)
        {
          domainEvents.Add(MatchEvent.Create(match.Id, clubId, playerOut, MatchEventType.SubstitutionOut, minute));
        }

        continue;
      }

      var eventType = SoccerDataMatchEventTypeMapper.Map(soccerDataEvent.EventType);
      if (eventType is null)
      {
        logger.LogWarning(
          "Skipping SoccerData event with unknown type {EventType} for MatchId={MatchId}",
          soccerDataEvent.EventType,
          match.Id);
        continue;
      }

      var player = await GetOrCreatePlayerAsync(db, soccerDataEvent.Player, cancellationToken)
        .ConfigureAwait(false);
      if (player is null)
        continue;

      domainEvents.Add(MatchEvent.Create(match.Id, clubId, player, eventType.Value, minute));

      if (eventType is MatchEventType.Goal or MatchEventType.PenaltyGoal)
      {
        var assistPlayer = await GetOrCreatePlayerAsync(
            db,
            soccerDataEvent.AssistPlayer,
            cancellationToken)
          .ConfigureAwait(false);
        if (assistPlayer is not null)
        {
          domainEvents.Add(MatchEvent.Create(match.Id, clubId, assistPlayer, MatchEventType.Assist, minute));
        }
      }
    }

    if (domainEvents.Count == 0)
      return 0;

    await db.MatchEvent.AddRangeAsync(domainEvents, cancellationToken).ConfigureAwait(false);
    return domainEvents.Count;
  }

  private static bool TryParseMinute(string? eventMinute, out int minute)
  {
    minute = 0;
    if (string.IsNullOrWhiteSpace(eventMinute))
      return false;

    var mainPart = eventMinute.Split('+', 2)[0].Trim();
    return int.TryParse(mainPart, out minute);
  }

  private static bool TryResolveClubId(string team, int homeClubId, int awayClubId, out int clubId)
  {
    if (team.Equals("home", StringComparison.OrdinalIgnoreCase))
    {
      clubId = homeClubId;
      return true;
    }

    if (team.Equals("away", StringComparison.OrdinalIgnoreCase))
    {
      clubId = awayClubId;
      return true;
    }

    clubId = 0;
    return false;
  }

  private static async Task<DomainPlayer?> GetOrCreatePlayerAsync(
    AppDbContext db,
    SoccerDataPlayer? soccerDataPlayer,
    CancellationToken cancellationToken)
  {
    if (soccerDataPlayer is null || soccerDataPlayer.Id <= 0 || string.IsNullOrWhiteSpace(soccerDataPlayer.Name))
      return null;

    var existing = db.Player.Local.FirstOrDefault(p => p.SoccerdataId == soccerDataPlayer.Id)
      ?? await db.Player
        .FirstOrDefaultAsync(p => p.SoccerdataId == soccerDataPlayer.Id, cancellationToken)
        .ConfigureAwait(false);
    if (existing is not null)
      return existing;

    var player = new DomainPlayer
    {
      SoccerdataId = soccerDataPlayer.Id,
      Name = soccerDataPlayer.Name.Trim()
    };
    db.Player.Add(player);
    return player;
  }
}
