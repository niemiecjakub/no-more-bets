using MediatR;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Command to refresh a single match preview from SoccerData API and upsert into the database.</summary>
public record RefreshSoccerDataMatchPreviewCommand(int MatchId) : IRequest<Unit>;
