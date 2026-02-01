using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Query to fetch a single match preview from SoccerData API.</summary>
public record GetSoccerDataMatchPreviewQuery(int MatchId) : IRequest<MatchPreview>;
