using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;

/// <summary>
/// Query to fetch the league table from FotMob, optionally filtered by home/away/form.
/// </summary>
public record GetFotmobLeagueTableQuery(TableFilter Filter = TableFilter.All) : IRequest<IReadOnlyList<ClubDto>>;
