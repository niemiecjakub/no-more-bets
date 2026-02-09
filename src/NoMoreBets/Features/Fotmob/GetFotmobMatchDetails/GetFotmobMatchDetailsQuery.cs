using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;

/// <summary>
/// Query to fetch match details (general info and lineups) from a FotMob match detail page.
/// </summary>
/// <param name="GameUrl">FotMob match page URL.</param>
public record GetFotmobMatchDetailsQuery(string GameUrl) : IRequest<MatchDetailsDto>;
