using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Query to fetch upcoming match previews (from API and persisted to DB). Returns empty list if none.</summary>
public record GetSoccerDataMatchPreviewsUpcomingQuery(int? LeagueId = null) : IRequest<IReadOnlyList<LeagueMatchPreviews>>;
