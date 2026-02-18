using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Query to fetch a single match preview from the database (cached). Returns null if not found.</summary>
public record GetSoccerDataMatchPreviewQuery(int MatchId) : IRequest<MatchPreview?>;
