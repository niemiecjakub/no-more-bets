using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Query to fetch upcoming match previews from SoccerData API, optionally filtered by league ID.</summary>
public record GetSoccerDataMatchPreviewsUpcomingQuery(int? LeagueId = null) : IRequest<IReadOnlyList<LeagueMatchPreviews>>;
