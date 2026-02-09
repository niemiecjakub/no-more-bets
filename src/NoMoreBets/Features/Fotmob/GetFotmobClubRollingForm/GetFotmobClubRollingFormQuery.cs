using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm;

/// <summary>
/// Query to fetch rolling form (averages over last 5 games) for a club from FotMob.
/// </summary>
/// <param name="TeamId">FotMob team ID (for club overview).</param>
/// <param name="TeamName">Team name as used on match pages (for core match details).</param>
public record GetFotmobClubRollingFormQuery(int TeamId, string TeamName) : IRequest<ClubRollingFormDto>;
