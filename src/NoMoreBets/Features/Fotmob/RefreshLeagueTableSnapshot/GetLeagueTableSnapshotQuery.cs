using MediatR;
using NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot.Dtos;

namespace NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;

/// <summary>Query to get a league table snapshot by season and optional date (latest if null). When SeasonId is null, uses latest season (max id) for the league.</summary>
public record GetLeagueTableSnapshotQuery(int LeagueId, int? SeasonId = null, DateOnly? SnapshotDate = null)
  : IRequest<LeagueTableSnapshotDto?>;
