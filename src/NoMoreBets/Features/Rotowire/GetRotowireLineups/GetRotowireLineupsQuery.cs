using MediatR;
using NoMoreBets.Domain.Entities.Rotowire;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>
/// Query to fetch soccer lineups from RotoWire.
/// </summary>
public record GetRotowireLineupsQuery : IRequest<IReadOnlyList<GameLineup>>;
