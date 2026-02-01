using MediatR;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents;

/// <summary>
/// Query to fetch bookmaker events (markets) for a specific match from Betclic.
/// </summary>
/// <param name="GameUrl">URL to the match page.</param>
/// <param name="Expand">If true, clicks consent/modal and "see more" before parsing.</param>
public record GetBetclicMatchEventsQuery(string GameUrl, bool Expand = false) : IRequest<IReadOnlyList<BookmakerEvent>>;
