using MediatR;
using NoMoreBets.Domain.Entity;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Command to refresh upcoming match previews from SoccerData API, sync Match table, and update cache.</summary>
public record RefreshSoccerDataMatchPreviewsUpcomingCommand(int? SoccerdataLeagueId = null) : IRequest<List<Match>>;
