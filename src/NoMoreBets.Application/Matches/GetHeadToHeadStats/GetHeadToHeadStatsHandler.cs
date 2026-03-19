using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Matches.GetHeadToHeadStats;

public record GetHeadToHeadStatsQuery(int MatchId) : IRequest<H2H?>;

public sealed class GetHeadToHeadStatsHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetHeadToHeadStatsQuery, H2H?>
{
  public async Task<H2H?> Handle(GetHeadToHeadStatsQuery request, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(request.MatchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return null;

    var head2head = await unitOfWork.Matches.GetHeadToHead(match.HomeClubId, match.AwayClubId).ConfigureAwait(false);
    if (head2head == null || string.IsNullOrWhiteSpace(head2head.Head2HeadJson))
      return null;

    var dto = head2head.GetHeadToHead();
    if (dto == null || dto.Stats?.Overall == null)
      return null;

    return MapToH2H(dto, match, head2head);
  }

  private static H2H MapToH2H(HeadToHead dto, Match match, Head2Head entity)
  {
    var overall = dto.Stats.Overall;
    var totalMatches = overall.OverallGamesPlayed;
    var homeIsTeam1 = match.HomeClubId == entity.Team1Id;

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
