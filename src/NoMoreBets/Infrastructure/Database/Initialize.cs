using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Features.SoccerData.Model;
using System.Globalization;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using DomainSeason = NoMoreBets.Domain.Leagues.Season;
using DomainStage = NoMoreBets.Domain.Matches.Stage;

namespace NoMoreBets.Infrastructure.Database;

public class Initialize
{
  private readonly AppDbContext _context;
  public Initialize(AppDbContext context)
  {
    _context = context;
  }

  public async Task SeedMatchData(IReadOnlyList<LeagueMatches> matches)
  {
    if (matches.Count == 0)
    {
      return;
    }

    var leaguesFromPayload = matches
      .GroupBy(m => m.LeagueId)
      .Select(g => new League
      {
        SoccerdataId = g.Key,
        Name = g.First().LeagueName
      })
      .ToList();

    var leagueIds = leaguesFromPayload.Select(l => l.SoccerdataId).ToHashSet();
    var existingLeagues = await _context.League
      .Where(l => leagueIds.Contains(l.SoccerdataId))
      .ToListAsync();

    var existingLeagueSoccerDataIds = existingLeagues.Select(l => l.SoccerdataId).ToHashSet();
    var leaguesToInsert = leaguesFromPayload
      .Where(l => !existingLeagueSoccerDataIds.Contains(l.SoccerdataId))
      .ToList();

    if (leaguesToInsert.Count > 0)
    {
      await _context.League.AddRangeAsync(leaguesToInsert);
      await _context.SaveChangesAsync();
      existingLeagues.AddRange(leaguesToInsert);
    }

    var leagueDbIdBySoccerDataId = existingLeagues.ToDictionary(l => l.SoccerdataId, l => l.Id);

    var clubsFromPayload = matches
      .SelectMany(m => m.Stage.SelectMany(s => s.Matches.SelectMany(match =>
        new[]
        {
          new Club
          {
            Name = match.Teams.Home.Name,
            SoccerdataId = match.Teams.Home.Id,
            LeagueId = leagueDbIdBySoccerDataId[m.LeagueId]
          },
          new Club
          {
            Name = match.Teams.Away.Name,
            SoccerdataId = match.Teams.Away.Id,
            LeagueId = leagueDbIdBySoccerDataId[m.LeagueId]
          }
        })))
      .Where(c => c.Name != "None")
      .GroupBy(c => c.SoccerdataId)
      .Select(g => g.First())
      .ToList();

    var clubIds = clubsFromPayload.Select(c => c.SoccerdataId).ToHashSet();
    var existingClubSoccerDataIds = await _context.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .Select(c => c.SoccerdataId)
      .ToListAsync();

    var existingClubIdSet = existingClubSoccerDataIds.ToHashSet();
    var clubsToInsert = clubsFromPayload
      .Where(c => !existingClubIdSet.Contains(c.SoccerdataId))
      .ToList();

    if (clubsToInsert.Count > 0)
    {
      await _context.Club.AddRangeAsync(clubsToInsert);
      await _context.SaveChangesAsync();
    }

    var clubDbIdBySoccerDataId = await _context.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToDictionaryAsync(c => c.SoccerdataId, c => c.Id);

    var seasonsFromPayload = matches
      .Where(m => leagueDbIdBySoccerDataId.ContainsKey(m.LeagueId) && !string.IsNullOrWhiteSpace(m.Season.Year))
      .GroupBy(m => new { LeagueId = leagueDbIdBySoccerDataId[m.LeagueId], m.Season.Year })
      .Select(g => new DomainSeason
      {
        LeagueId = g.Key.LeagueId,
        Year = g.Key.Year,
      })
      .ToList();

    if (seasonsFromPayload.Count > 0)
    {
      var seasonLeagueIds = seasonsFromPayload.Select(s => s.LeagueId).ToHashSet();
      var existingSeasons = await _context.Season
        .Where(s => seasonLeagueIds.Contains(s.LeagueId))
        .ToListAsync();

      var existingSeasonByKey = existingSeasons.ToDictionary(s => (s.LeagueId, s.Year), s => s);
      var seasonsToInsert = new List<DomainSeason>();

      foreach (var season in seasonsFromPayload)
      {
        var key = (season.LeagueId, season.Year);
        if (existingSeasonByKey.ContainsKey(key))
        {
          continue;
        }

        seasonsToInsert.Add(season);
        existingSeasonByKey[key] = season;
      }

      if (seasonsToInsert.Count > 0)
      {
        await _context.Season.AddRangeAsync(seasonsToInsert);
        await _context.SaveChangesAsync();
      }
    }

    var seasonCandidates = matches
      .Where(m => leagueDbIdBySoccerDataId.ContainsKey(m.LeagueId) && !string.IsNullOrWhiteSpace(m.Season.Year))
      .Select(m => new { LeagueId = leagueDbIdBySoccerDataId[m.LeagueId], m.Season.Year })
      .Distinct()
      .ToList();

    var seasonDbIdByLeagueAndYear = seasonCandidates.Count == 0
      ? new Dictionary<(int LeagueId, string Year), int>()
      : await _context.Season
        .Where(s => seasonCandidates.Select(x => x.LeagueId).Contains(s.LeagueId))
        .ToDictionaryAsync(s => (s.LeagueId, s.Year), s => s.Id);

    var stagesFromPayload = matches
      .SelectMany(m => m.Stage.Select(s => new
      {
        LeagueId = leagueDbIdBySoccerDataId.GetValueOrDefault(m.LeagueId),
        SeasonYear = m.Season.Year,
        StageSoccerdataId = s.StageId,
        StageName = s.StageName,
      }))
      .Where(x => !string.IsNullOrWhiteSpace(x.SeasonYear) && !string.IsNullOrWhiteSpace(x.StageName))
      .Select(x =>
      {
        if (!seasonDbIdByLeagueAndYear.TryGetValue((x.LeagueId, x.SeasonYear), out var seasonId))
        {
          return null;
        }

        return new DomainStage
        {
          SeasonId = seasonId,
          SoccerdataId = x.StageSoccerdataId,
          Name = x.StageName,
        };
      })
      .Where(s => s is not null)
      .Select(s => s!)
      .GroupBy(s => new { s.SeasonId, s.SoccerdataId, s.Name })
      .Select(g => new DomainStage
      {
        SeasonId = g.Key.SeasonId,
        SoccerdataId = g.Key.SoccerdataId,
        Name = g.Key.Name,
      })
      .ToList();

    if (stagesFromPayload.Count > 0)
    {
      var stageSeasonIds = stagesFromPayload.Select(s => s.SeasonId).ToHashSet();
      var existingStages = await _context.Stage
        .Where(s => stageSeasonIds.Contains(s.SeasonId))
        .ToListAsync();

      var existingStageByKey = existingStages.ToDictionary(s => (s.SeasonId, s.Name), s => s);
      var stagesToInsert = new List<DomainStage>();

      foreach (var stage in stagesFromPayload)
      {
        var key = (stage.SeasonId, stage.Name);
        if (existingStageByKey.ContainsKey(key))
        {
          continue;
        }

        stagesToInsert.Add(stage);
        existingStageByKey[key] = stage;
      }

      if (stagesToInsert.Count > 0)
      {
        await _context.Stage.AddRangeAsync(stagesToInsert);
        await _context.SaveChangesAsync();
      }
    }

    var stageCandidates = stagesFromPayload
      .Select(s => new { s.SeasonId, s.Name })
      .Distinct()
      .ToList();

    var stageDbIdBySeasonAndName = stageCandidates.Count == 0
      ? new Dictionary<(int SeasonId, string Name), int>()
      : await _context.Stage
        .Where(s => stageCandidates.Select(x => x.SeasonId).Contains(s.SeasonId))
        .ToDictionaryAsync(s => (s.SeasonId, s.Name), s => s.Id);

    var matchesFromPayload = new List<DomainMatch>();

    foreach (var leagueMatches in matches)
    {
      if (!leagueDbIdBySoccerDataId.TryGetValue(leagueMatches.LeagueId, out var leagueDbId))
      {
        continue;
      }

      if (!seasonDbIdByLeagueAndYear.TryGetValue((leagueDbId, leagueMatches.Season.Year), out var seasonId))
      {
        continue;
      }

      foreach (var stage in leagueMatches.Stage)
      {
        if (!stageDbIdBySeasonAndName.TryGetValue((seasonId, stage.StageName), out var stageId))
        {
          continue;
        }

        foreach (var match in stage.Matches)
        {
          if (!clubDbIdBySoccerDataId.TryGetValue(match.Teams.Home.Id, out var homeClubId))
          {
            continue;
          }

          if (!clubDbIdBySoccerDataId.TryGetValue(match.Teams.Away.Id, out var awayClubId))
          {
            continue;
          }

          matchesFromPayload.Add(new DomainMatch
          {
            SoccerdataId = match.Id,
            StageId = stageId,
            MatchDate = DateTime.SpecifyKind(
              DateTime.ParseExact($"{match.Date} {match.Time}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
              DateTimeKind.Utc),
            HomeClubId = homeClubId,
            AwayClubId = awayClubId,
            MatchStatusId = string.Equals("finished", match.Status) ? (int)MatchStatus.Finished : (int)MatchStatus.Upcomming,
            HomeGoals = string.Equals("finished", match.Status) ? match.Goals.HomeFtGoals : null,
            AwayGoals = string.Equals("finished", match.Status) ? match.Goals.AwayFtGoals : null,
          });
        }
      }
    }

    var uniqueMatchesFromPayload = matchesFromPayload
      .GroupBy(m => m.SoccerdataId)
      .Select(g => g.OrderByDescending(x => x.MatchDate).First())
      .ToList();

    if (uniqueMatchesFromPayload.Count == 0)
    {
      return;
    }

    var soccerdataMatchIds = uniqueMatchesFromPayload.Select(m => m.SoccerdataId).ToHashSet();
    var existingMatchesBySoccerdataId = await _context.Match
      .Where(m => soccerdataMatchIds.Contains(m.SoccerdataId))
      .ToDictionaryAsync(m => m.Id);

    var matchesToInsert = new List<DomainMatch>();
    foreach (var payloadMatch in uniqueMatchesFromPayload)
    {
      if (existingMatchesBySoccerdataId.TryGetValue(payloadMatch.Id, out var existingMatch))
      {
        existingMatch.SoccerdataId = payloadMatch.SoccerdataId;
        existingMatch.StageId = payloadMatch.StageId;
        existingMatch.MatchDate = payloadMatch.MatchDate;
        existingMatch.HomeClubId = payloadMatch.HomeClubId;
        existingMatch.AwayClubId = payloadMatch.AwayClubId;
        existingMatch.MatchStatusId = payloadMatch.MatchStatusId;
        existingMatch.HomeGoals = payloadMatch.HomeGoals;
        existingMatch.AwayGoals = payloadMatch.AwayGoals;
        continue;
      }

      matchesToInsert.Add(payloadMatch);
    }

    if (matchesToInsert.Count > 0)
    {
      await _context.Match.AddRangeAsync(matchesToInsert);
      await _context.SaveChangesAsync();
    }
  }
}
