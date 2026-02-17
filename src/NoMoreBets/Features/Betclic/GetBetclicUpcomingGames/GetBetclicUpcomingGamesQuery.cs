using MediatR;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;

namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;

/// <summary>
/// Query to fetch upcoming Premier League games from Betclic.
/// </summary>
public record GetBetclicUpcomingGamesQuery : IRequest<IReadOnlyList<UpcomingGameDto>>;
