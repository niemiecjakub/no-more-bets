using MediatR;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>Command to refresh Rotowire lineups (scrape and persist to database).</summary>
public record RefreshRotowireLineupsCommand : IRequest<Unit>;
