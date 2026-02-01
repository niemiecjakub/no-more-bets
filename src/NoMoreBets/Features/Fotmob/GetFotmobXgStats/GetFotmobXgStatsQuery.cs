using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobXgStats;

/// <summary>
/// Query to fetch xG statistics table from FotMob.
/// </summary>
public record GetFotmobXgStatsQuery : IRequest<IReadOnlyList<XgStatsDto>>;
