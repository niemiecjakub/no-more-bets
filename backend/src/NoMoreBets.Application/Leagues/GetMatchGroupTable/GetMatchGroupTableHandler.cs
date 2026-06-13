using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Leagues.GetMatchGroupTable;

public record GetMatchGroupTableQuery(int MatchId) : IRequest<IReadOnlyList<LeagueTableStanding>?>;

public sealed class GetMatchGroupTableHandler(
  IUnitOfWork unitOfWork,
  WorldCupGroupRegistry worldCupGroupRegistry,
  ILogger<GetMatchGroupTableHandler>? logger = null)
  : IRequestHandler<GetMatchGroupTableQuery, IReadOnlyList<LeagueTableStanding>?>
{
  private readonly ILogger<GetMatchGroupTableHandler> _logger = logger ?? NullLogger<GetMatchGroupTableHandler>.Instance;

  public async Task<IReadOnlyList<LeagueTableStanding>?> Handle(
    GetMatchGroupTableQuery request,
    CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match?.Stage?.Season?.League is not { } league)
    {
      _logger.LogWarning("Cannot load group table because league context is missing for match {MatchId}.", request.MatchId);
      return null;
    }

    if (!match.IsFifaWorldCup)
      return null;

    var homeGroup = worldCupGroupRegistry.GetGroupForClubName(match.HomeClub.Name);
    if (homeGroup is null)
    {
      _logger.LogWarning(
        "Cannot load group table because home club {ClubName} has no World Cup group for match {MatchId}.",
        match.HomeClub.Name,
        request.MatchId);
      return null;
    }

    var awayGroup = worldCupGroupRegistry.GetGroupForClubName(match.AwayClub.Name);
    if (awayGroup is not null
        && !string.Equals(homeGroup.Code, awayGroup.Code, StringComparison.OrdinalIgnoreCase))
    {
      _logger.LogWarning(
        "Home club {HomeClub} (group {HomeGroup}) and away club {AwayClub} (group {AwayGroup}) are in different groups for match {MatchId}.",
        match.HomeClub.Name,
        homeGroup.Code,
        match.AwayClub.Name,
        awayGroup.Code,
        request.MatchId);
    }

    var standings = await unitOfWork.Leagues
      .GetLeagueTableAsOfAsync(league.Id, asOfDate: null, cancellationToken)
      .ConfigureAwait(false);

    if (standings is null)
      return null;

    return standings
      .Where(s => worldCupGroupRegistry.IsClubInGroup(s.ClubName, homeGroup.Code))
      .OrderBy(s => s.Stats.Position)
      .ToList();
  }
}
