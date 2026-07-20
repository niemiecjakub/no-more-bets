using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Matches.GetHeadToHeadStats;

public record GetHeadToHeadStatsQuery(int MatchId) : IRequest<H2H?>;

public sealed class GetHeadToHeadStatsHandler(IUnitOfWork unitOfWork, ILogger<GetHeadToHeadStatsHandler>? logger = null) : IRequestHandler<GetHeadToHeadStatsQuery, H2H?>
{
  public async Task<H2H?> Handle(GetHeadToHeadStatsQuery request, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(request.MatchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
    {
      logger?.LogWarning("Match {MatchId} not found for head-to-head stats.", request.MatchId);
      return null;
    }

    var head2head = await unitOfWork.Matches.GetHeadToHead(match.HomeClubId, match.AwayClubId).ConfigureAwait(false);
    if (head2head == null || string.IsNullOrWhiteSpace(head2head.Head2HeadJson))
    {
      logger?.LogWarning("No head-to-head payload found for match {MatchId}.", request.MatchId);
      return null;
    }

    var dto = head2head.GetHeadToHead();
    if (dto == null || dto.Stats?.Overall == null)
    {
      logger?.LogWarning("Head-to-head payload is malformed for match {MatchId}.", request.MatchId);
      return null;
    }

    try
    {
      return MapToH2H(dto, match);
    }
    catch (Exception ex)
    {
      logger?.LogError(ex, "Failed to map head-to-head stats for match {MatchId}.", request.MatchId);
      throw;
    }
  }

  private static H2H MapToH2H(HeadToHead dto, Match match)
  {
    var overall = dto.Stats.Overall;
    var totalMatches = overall.OverallGamesPlayed;
    // JSON team1/team2 follow SoccerData API ids (request order), not Head2Head.Team1Id/Team2Id
    // which are normalized internal club ids. Match home/away via SoccerdataId.
    var homeIsTeam1 = match.HomeClub.SoccerdataId == dto.Team1.Id;

    TeamMetrics teamA;
    TeamMetrics teamB;
    if (homeIsTeam1)
    {
      teamA = BuildTeamMetrics(
        match.HomeClub.Name,
        overall.OverallTeam1Wins,
        overall.OverallTeam1Scored,
        overall.OverallTeam2Scored,
        dto.Stats.Team1AtHome.Team1WinsAtHome,
        dto.Stats.Team2AtHome.Team2LossesAtHome,
        totalMatches);
      teamB = BuildTeamMetrics(
        match.AwayClub.Name,
        overall.OverallTeam2Wins,
        overall.OverallTeam2Scored,
        overall.OverallTeam1Scored,
        dto.Stats.Team2AtHome.Team2WinsAtHome,
        dto.Stats.Team1AtHome.Team1LossesAtHome,
        totalMatches);
    }
    else
    {
      teamA = BuildTeamMetrics(
        match.HomeClub.Name,
        overall.OverallTeam2Wins,
        overall.OverallTeam2Scored,
        overall.OverallTeam1Scored,
        dto.Stats.Team2AtHome.Team2WinsAtHome,
        dto.Stats.Team1AtHome.Team1LossesAtHome,
        totalMatches);
      teamB = BuildTeamMetrics(
        match.AwayClub.Name,
        overall.OverallTeam1Wins,
        overall.OverallTeam1Scored,
        overall.OverallTeam2Scored,
        dto.Stats.Team1AtHome.Team1WinsAtHome,
        dto.Stats.Team2AtHome.Team2LossesAtHome,
        totalMatches);
    }

    return new H2H
    {
      Summary = $"{match.HomeClub.Name} vs {match.AwayClub.Name}",
      TotalMatches = totalMatches,
      TotalDraws = overall.OverallDraws,
      TeamA = teamA,
      TeamB = teamB
    };
  }

  private static TeamMetrics BuildTeamMetrics(
    string name,
    int totalWins,
    int totalGoalsScored,
    int totalGoalsConceded,
    int homeWins,
    int awayWins,
    int totalMatches)
  {
    return new TeamMetrics
    {
      Name = name,
      TotalWins = totalWins,
      TotalGoalsScored = totalGoalsScored,
      TotalGoalsConceded = totalGoalsConceded,
      HomeWins = homeWins,
      AwayWins = awayWins,
      WinPercentage = totalMatches > 0 ? totalWins * 100.0 / totalMatches : 0,
      AvgGoalsScored = totalMatches > 0 ? (double)totalGoalsScored / totalMatches : 0,
      AvgGoalsConceded = totalMatches > 0 ? (double)totalGoalsConceded / totalMatches : 0
    };
  }
}
