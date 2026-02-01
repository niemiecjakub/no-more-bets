using MediatR;
using NoMoreBets.Features.Rotowire.Model;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>
/// Query to fetch soccer lineups from RotoWire.
/// </summary>
public record GetRotowireLineupsQuery : IRequest<IReadOnlyList<GameLineup>>;
