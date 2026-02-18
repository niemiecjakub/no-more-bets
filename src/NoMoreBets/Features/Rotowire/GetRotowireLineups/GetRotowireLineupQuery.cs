using MediatR;
using NoMoreBets.Features.Rotowire.Model;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>Query to fetch a single lineup from the database by SoccerData match id. Returns null if not found.</summary>
public record GetRotowireLineupQuery(int SoccerDataMatchId) : IRequest<GameLineup?>;
