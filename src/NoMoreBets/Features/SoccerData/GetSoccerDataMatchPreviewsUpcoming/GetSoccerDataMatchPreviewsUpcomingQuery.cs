using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

public record GetSoccerDataMatchPreviewsUpcomingQuery(int? LeagueId = null) : IRequest<IReadOnlyList<LeagueMatchPreviews>>;
