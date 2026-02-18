using MediatR;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

/// <summary>Command to refresh head-to-head data from SoccerData API and upsert into the database.</summary>
public record RefreshSoccerDataHeadToHeadCommand(int Team1Id, int Team2Id) : IRequest<Unit>;
