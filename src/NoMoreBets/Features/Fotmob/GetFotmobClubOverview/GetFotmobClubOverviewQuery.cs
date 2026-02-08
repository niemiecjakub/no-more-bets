using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubOverview;

/// <summary>
/// Query to fetch club overview (recent games and daily summary) from FotMob team overview page.
/// </summary>
public record GetFotmobClubOverviewQuery(int TeamId) : IRequest<ClubOverviewDto>;
