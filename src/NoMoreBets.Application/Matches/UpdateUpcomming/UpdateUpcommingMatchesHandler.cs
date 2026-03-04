using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Matches;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateUpcomming;

/// <summary>Command to refresh upcoming match previews from SoccerData API, sync Match table, and update cache.</summary>
public record UpdateUpcommingMatchesCommand(int? SoccerdataLeagueId = null) : IRequest<List<Match>>;

public class UpdateUpcommingMatchesHandler(
  IUpcommingMatchProvider upcommingMatchProvider,
  IMatchMatcher matchMatcher,
  IUnitOfWork unitOfWork,
  IMatchRepository matchRepository,
  ILogger<UpdateUpcommingMatchesHandler> logger)
  : IRequestHandler<UpdateUpcommingMatchesCommand, List<Match>>
{
  public async Task<List<Match>> Handle(UpdateUpcommingMatchesCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Soccerdata league {SoccerdataLeagueId}",
      nameof(UpdateUpcommingMatchesHandler),
      request.SoccerdataLeagueId);

    var added = new List<Match>();
    var previews = await upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(request.SoccerdataLeagueId);

    logger.LogInformation(
      "Handler {HandlerName} fetched {LeagueCount} leagues with upcoming match previews from SoccerData",
      nameof(UpdateUpcommingMatchesHandler),
      previews.Count);

    var clubIds = previews
      .SelectMany(l => l.MatchPreviews)
      .SelectMany(p => new[] { p.Teams.Home.Id, p.Teams.Away.Id })
      .Distinct()
      .ToList();

    var clubsBySoccerdataId = await unitOfWork.Clubs.GetBySoccerdataId(clubIds);
    var clubsMap = clubsBySoccerdataId.ToDictionary(c => c.SoccerdataId, c => c);

    var leagues = await unitOfWork.Leagues.GetLeagues();
    var leagueIds = leagues.Select(c => c.SoccerdataId);
    foreach (var league in previews)
    {
      if (!leagueIds.Contains(league.LeagueId))
      {
        continue;
      }

      var currentStage = await unitOfWork.Leagues.GetCurrentStage(league.LeagueId);
      foreach (var matchPreview in league.MatchPreviews)
      {
        if (!TryParseMatchDate(matchPreview.Date, matchPreview.Time, out var gameDayUtc))
        {
          continue;
        }

        var matchesOnDay = await matchRepository.GetMatches(gameDayUtc);

        var candidates = matchesOnDay
          .Select(m => (m.HomeClub.Name, m.AwayClub.Name, (Match)m))
          .ToList();

        var matched = matchMatcher.FindBestMatch(matchPreview.Teams.Home.Name, matchPreview.Teams.Away.Name, candidates);

        if (matched is not null)
        {
          if (matched.SoccerdataId is null)
          {
            matched.SoccerdataId = matchPreview.Id;
          }
          continue;
        }

        if (!clubsMap.TryGetValue(matchPreview.Teams.Home.Id, out var homeClub) ||
            !clubsMap.TryGetValue(matchPreview.Teams.Away.Id, out var awayClub))
        {
          logger.LogWarning(
            "Skipping insert for match {MatchId} ({Home} vs {Away}): missing club(s) in DB. HomeClubId={HomeSoccerdataId}, AwayClubId={AwaySoccerdataId}",
            matchPreview.Id,
            matchPreview.Teams.Home.Name,
            matchPreview.Teams.Away.Name,
            matchPreview.Teams.Home.Id,
            matchPreview.Teams.Away.Id);
          continue;
        }

        var newMatch = Match.CreateUpcomming(gameDayUtc, currentStage.Id, homeClub.Id, awayClub.Id);
        newMatch.SoccerdataId = matchPreview.Id;
        await unitOfWork.Matches.AddMatch(newMatch);
        added.Add(newMatch);
      }
    }

    await unitOfWork.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
      "Handler {HandlerName} completed. Added {AddedMatchCount} new matches for Soccerdata league {SoccerdataLeagueId}",
      nameof(UpdateUpcommingMatchesHandler),
      added.Count,
      request.SoccerdataLeagueId);
    return added;
  }

  private static bool TryParseMatchDate(string dateStr, string timeStr, out DateTime gameDayUtc)
  {
    gameDayUtc = default;
    if (string.IsNullOrWhiteSpace(dateStr))
    {
      return false;
    }

    if (!DateTime.TryParseExact(
      $"{dateStr.Trim()} {timeStr?.Trim() ?? "00:00"}",
      new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm", "dd/MM/yyyy" },
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out var parsed))
    {
      return false;
    }

    gameDayUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    return true;
  }
}
