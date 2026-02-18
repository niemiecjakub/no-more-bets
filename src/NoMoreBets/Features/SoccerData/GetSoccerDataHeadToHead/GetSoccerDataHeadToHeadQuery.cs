using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

/// <summary>Query to fetch head-to-head data between two teams from the database (cached). Returns null if not found.</summary>
public record GetSoccerDataHeadToHeadQuery(int Team1Id, int Team2Id) : IRequest<HeadToHead?>;
