using MediatR;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;

/// <summary>
/// Query to fetch upcoming Premier League games from Betclic.
/// </summary>
public record GetBetclicUpcomingGamesQuery : IRequest<IReadOnlyList<UpcomingGame>>;
