using MediatR;

namespace NoMoreBets.Features.Fotmob.UpdateFotmobRecentMatches;

/// <summary>Command to refresh Fotmob match details from a club's recent games: fetch overview, scrape details for new URLs, fuzzy-match to Match, and insert MatchDetails.</summary>
public record UpdateFotmobRecentMatchesCommand(int TeamId) : IRequest<Unit>;
