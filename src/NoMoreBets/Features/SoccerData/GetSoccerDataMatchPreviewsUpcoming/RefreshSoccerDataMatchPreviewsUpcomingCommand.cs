using MediatR;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Command to refresh upcoming match previews from SoccerData API, sync Match table, and update cache.</summary>
public record RefreshSoccerDataMatchPreviewsUpcomingCommand(int? LeagueId = null) : IRequest<Unit>;
