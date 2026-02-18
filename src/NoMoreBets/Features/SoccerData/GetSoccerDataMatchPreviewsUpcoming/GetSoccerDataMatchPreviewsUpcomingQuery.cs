using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Query to fetch upcoming match previews from the database (cached). Returns null if not found.</summary>
public record GetSoccerDataMatchPreviewsUpcomingQuery(int? LeagueId = null) : IRequest<IReadOnlyList<LeagueMatchPreviews>?>;
